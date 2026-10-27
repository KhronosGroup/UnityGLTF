using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLTF
{
	// Static class for construction GLB objects. These API's only work with the .NET 4.6 runtime and above.
	public static class GLBBuilder
	{
		/// <summary>
		/// Turns a glTF file w/ structure into a GLB. Does not currently copy binary data
		/// </summary>
		/// <param name="root">The glTF root to turn into a GLBObject</param>
		/// <param name="glbOutStream">Output stream to write the GLB to</param>
		/// <param name="loader">Loader for loading external components from GLTFRoot. The loader will receive uris and return the stream to the resource</param>
		/// <param name="glbVersion">The GLB binary format version to output.</param>
		/// <returns>A constructed GLBObject</returns>
		private static GLBObject ConstructFromGLTF(GLTFRoot root, Stream glbOutStream, Func<string, Stream> loader, uint glbVersion = 2)
		{
			if (root == null) throw new ArgumentNullException(nameof(root));
			if (glbOutStream == null) throw new ArgumentNullException(nameof(glbOutStream));
			
			MemoryStream gltfJsonStream = new MemoryStream();
			using (StreamWriter sw = new StreamWriter(gltfJsonStream))
			{
				root.Serialize(sw, true);
				sw.Flush();
				GLBHeader glbHeader = new GLBHeader
				{
					Version = glbVersion,
				};
				long proposedFileLength = gltfJsonStream.Length + glbHeader.GetFileHeaderSize() + glbHeader.GetChunkHeaderSize();
				ThrowIfTooLarge(glbHeader.Version, proposedFileLength);
				glbHeader.FileLength = proposedFileLength;
				glbOutStream.SetLength(proposedFileLength);
				GLBObject glbObject = new GLBObject
				{
					Header = glbHeader,
					Root = root,
					Stream = glbOutStream,
					JsonChunkInfo = new GLBChunkInfo
					{
						Length = gltfJsonStream.Length,
						StartPosition = glbHeader.GetFileHeaderSize(),
						Type = GLBChunkFormat.JSON
					}
				};

				// write header
				WriteHeader(glbOutStream, glbObject.Header, glbObject.StreamStartPosition);

				// write chunk header
				WriteChunkHeader(glbOutStream, glbObject.Header, glbObject.JsonChunkInfo);

				gltfJsonStream.Position = 0;
				gltfJsonStream.CopyTo(glbOutStream);

				// todo: implement getting binary data for loader

				return glbObject;
			}
		}

		/// <summary>
		/// Turns the GLB data contained in a stream into a GLBObject. Will copy to the outStream if specified
		/// </summary>
		/// <param name="root">The glTF root to turn into a GLBObject</param>
		/// <param name="inputGLBStream">The stream the glb came in</param>
		/// <param name="outStream">If outstream is specified, the glb gets copied to it</param>
		/// <param name="inputGLBStreamStartPosition">Offset into the buffer that the GLB starts</param>
		/// <returns>A constructed GLBObject</returns>
		public static GLBObject ConstructFromGLB(GLTFRoot root, Stream inputGLBStream, Stream outStream = null,
			long inputGLBStreamStartPosition = 0)
		{
			long streamStartPosition = inputGLBStreamStartPosition;
			if (outStream != null)
			{
				// the copy starts at the output stream's current position
				streamStartPosition = outStream.Position;
				inputGLBStream.Position = inputGLBStreamStartPosition;
				inputGLBStream.CopyTo(outStream);
			}
			else
			{
				outStream = inputGLBStream;
			}

			// header information is 4 bytes in, past the magic number
			outStream.Position = 4 + streamStartPosition;
			GLBHeader glbHeader = GLTFParser.ParseGLBHeader(outStream);

			// GLBObject models a JSON chunk followed by an optional BIN chunk, which is the only layout of version 2.
			// Other version 3 layouts can't be edited without losing chunks or their encoding.
			List<GLBChunkInfo> allChunks = GLTFParser.FindChunks(outStream, streamStartPosition);
			if (allChunks.Count == 0 || allChunks[0].Type != GLBChunkFormat.JSON)
			{
				throw new NotSupportedException("GLBBuilder only supports GLB files whose first chunk is the JSON chunk.");
			}
			if (allChunks.Count > 2 || (allChunks.Count == 2 && allChunks[1].Type != GLBChunkFormat.BIN))
			{
				throw new NotSupportedException("GLBBuilder only supports GLB files with a JSON chunk followed by an optional BIN chunk.");
			}
			foreach (GLBChunkInfo chunkInfo in allChunks)
			{
				if (chunkInfo.Encoding != 0)
				{
					throw new NotSupportedException("GLBBuilder only supports plainly encoded GLB chunks.");
				}
			}

			GLBChunkInfo jsonChunkInfo = allChunks[0];
			GLBChunkInfo binaryChunkInfo = allChunks.Count == 2
				? allChunks[1]
				: new GLBChunkInfo
				{
					Type = GLBChunkFormat.BIN
				};

			if (jsonChunkInfo.Length == 0)
			{
				throw new ArgumentException("JSON chunk must exists for valid GLB", nameof(inputGLBStream));
			}

			// todo: compute the initial bufferview list
			return new GLBObject
			{
				Root = root,
				Stream = outStream,
				StreamStartPosition = streamStartPosition,
				Header = glbHeader,
				JsonChunkInfo = jsonChunkInfo,
				BinaryChunkInfo = binaryChunkInfo
			};
		}

		/// <summary>
		/// Turns a stream that contains glTF or a GLB data into a GLBObject. glTF is not yet supported
		/// </summary>
		/// <param name="inStream">The stream to turn into a GLB</param>
		/// <param name="glbOutStream">If specified, output stream to write the GLB to</param>
		/// <param name="loader">Loader for loading external components from GLTFRoot. Loader is required if loading from glTF</param>
		/// <returns>A constructed GLBObject</returns>
		public static GLBObject ConstructFromStream(Stream inStream, Stream glbOutStream = null, Func<string, Stream> loader = null,
			long streamStartPosition = 0, bool removeUndefinedReferences = true)
		{
			if (inStream == null) throw new ArgumentNullException(nameof(inStream));

			if (inStream.Length > streamStartPosition)
			{
				inStream.Position = streamStartPosition;

				GLTFRoot root;
				GLTFParser.ParseJson(inStream, out root, streamStartPosition);
				if (removeUndefinedReferences)
				{
					GLTFHelpers.RemoveUndefinedReferences(root);
				}
				if (!root.IsGLB)
				{
					return ConstructFromGLTF(root, glbOutStream, loader);
				}

				return ConstructFromGLB(root, inStream, glbOutStream, streamStartPosition);
			}

			return _ConstructFromEmptyStream(inStream, streamStartPosition);
		}

		private static GLBObject _ConstructFromEmptyStream(Stream inStream, long streamStartPosition)
		{
			// Only support GLB version 2 for the empty stream case.
			GLBHeader glbHeader = new GLBHeader
			{
				Version = 2
			};
			glbHeader.FileLength = glbHeader.GetFileHeaderSize();
			GLBObject glbObject = new GLBObject
			{
				Stream = inStream,
				Header = glbHeader,
				JsonChunkInfo = new GLBChunkInfo
				{
					Length = 0,
					StartPosition = streamStartPosition + glbHeader.GetFileHeaderSize(),
					Type = GLBChunkFormat.JSON,
					Encoding = 0
				},
				BinaryChunkInfo = new GLBChunkInfo
				{
					Length = 0,
					StartPosition = streamStartPosition + glbHeader.GetFileHeaderSize() + glbHeader.GetChunkHeaderSize(),
					Type = GLBChunkFormat.BIN,
					Encoding = 0
				},
				StreamStartPosition = streamStartPosition
			};

			return glbObject;
		}

		/// <summary>
		/// Saves out the GLBObject to its own stream
		/// The GLBObject stream will be updated to be the output stream. Callers are responsible for handling Stream lifetime
		/// </summary>
		/// <param name="glb">The GLB to flush to the output stream and update</param>
		/// <returns>A GLBObject that is based upon outStream</returns>
		public static void UpdateStream(GLBObject glb)
		{
			if (glb.Root == null) throw new ArgumentException("glb Root and newRoot cannot be null", nameof(glb.Root));
			if (glb.Stream == null) throw new ArgumentException("glb GLBStream property cannot be null", nameof(glb.Stream));

			MemoryStream gltfJsonStream = new MemoryStream();
			using (StreamWriter sw = new StreamWriter(gltfJsonStream))
			{
				glb.Root.Serialize(sw, true);   // todo: this could out of memory exception
				sw.Flush();
				long jsonLength = gltfJsonStream.Length;
				if (glb.JsonChunkInfo.Length < jsonLength)
				{
					// allocate double what is required, so small changes don't move the binary chunk every time
					Relayout(glb, glb.Header.Version, jsonLength * 2);
				}

				// clear the buffer
				long jsonDataStartPosition = glb.JsonChunkInfo.StartPosition + glb.Header.GetChunkHeaderSize();
				glb.Stream.Position = jsonDataStartPosition;
				long amountToCopy = glb.JsonChunkInfo.Length;
				while (amountToCopy != 0)
				{
					int currAmountToCopy = (int)System.Math.Min(amountToCopy, (long)int.MaxValue);
					byte[] filler = Encoding.ASCII.GetBytes(new string(' ', currAmountToCopy));
					glb.Stream.Write(filler, 0, filler.Length);
					amountToCopy -= currAmountToCopy;
				}

				// write new JSON data
				gltfJsonStream.Position = 0;
				glb.Stream.Position = jsonDataStartPosition;
				gltfJsonStream.CopyTo(glb.Stream);
				glb.Stream.Flush();
			}
		}

		/// <summary>
		/// Adds binary data to the GLB
		/// </summary>
		/// <param name="glb">The glb to update</param>
		/// <param name="binaryData">The binary data to append</param>
		/// <param name="createBufferView">Whether a buffer view should be created, added to the GLTFRoot, and id returned</param>
		/// <param name="streamStartPosition">Start position of stream</param>
		/// <param name="bufferViewName">Root to replace the current one with</param>
		/// <returns>The location of the added buffer view</returns>
		public static BufferViewId AddBinaryData(GLBObject glb, Stream binaryData, bool createBufferView = true, long streamStartPosition = 0, string bufferViewName = null)
		{
			if (glb == null) throw new ArgumentNullException(nameof(glb));
			if (glb.Root == null && bufferViewName == null) throw new ArgumentException("glb Root and new root cannot be null", nameof(glb));
			if (glb.Stream == null) throw new ArgumentException("glb Stream cannot be null", nameof(glb));
			if (binaryData == null) throw new ArgumentNullException(nameof(binaryData));

			return _AddBinaryData(glb, binaryData, createBufferView, streamStartPosition, binaryData.Length - streamStartPosition, bufferViewName);
		}

		private static BufferViewId _AddBinaryData(GLBObject glb, Stream binaryData, bool createBufferView, long streamStartPosition, long dataLength, string bufferViewName = null)
		{
			long blobLength = CalculateAlignment(dataLength, glb.Header.GetAlignment());
			// there was an existing file that had no binary chunk info previously, append the chunk at the end
			if (glb.BinaryChunkInfo.Length == 0)
			{
				glb.SetBinaryChunkStartPosition(glb.StreamStartPosition + glb.Header.FileLength);
			}

			// assuming the binary chunk is the last chunk of the file
			long blobWritePosition = glb.BinaryChunkInfo.StartPosition + glb.Header.GetChunkHeaderSize() + glb.BinaryChunkInfo.Length;
			long newGLBSize = blobWritePosition + blobLength - glb.StreamStartPosition;
			ThrowIfTooLarge(glb.Header.Version, newGLBSize);
			glb.Stream.SetLength(glb.StreamStartPosition + newGLBSize); // zero-fills the padding after the blob
			CopyData(binaryData, streamStartPosition, glb.Stream, blobWritePosition, dataLength);

			glb.SetFileLength(newGLBSize);
			glb.SetBinaryChunkLength(glb.BinaryChunkInfo.Length + blobLength);

			// write glb header past magic number
			WriteHeader(glb.Stream, glb.Header, glb.StreamStartPosition);
			
			WriteChunkHeader(glb.Stream, glb.Header, glb.BinaryChunkInfo);

			if (createBufferView)
			{
				// Add a new BufferView to the GLTFRoot
				BufferView bufferView = new BufferView
				{
					Buffer = new BufferId
					{
						Id = 0,
						Root = glb.Root
					},
					ByteLength = blobLength, // figure out whether glb size is wrong or if documentation is unclear
					ByteOffset = glb.BinaryChunkInfo.Length - blobLength,
					Name = bufferViewName
				};

				if (glb.Root.BufferViews == null)
				{
					glb.Root.BufferViews = new List<BufferView>();
				}

				glb.Root.BufferViews.Add(bufferView);

				return new BufferViewId
				{
					Id = glb.Root.BufferViews.Count - 1,
					Root = glb.Root
				};
			}

			return null;
		}

		/// <summary>
		/// Merges two glb files together
		/// </summary>
		/// <param name="mergeTo">The glb to update</param>
		/// <param name="mergeFrom">The glb to merge from</param>
		public static void MergeGLBs(GLBObject mergeTo, GLBObject mergeFrom)
		{
			if (mergeTo == null) throw new ArgumentNullException(nameof(mergeTo));
			if (mergeFrom == null) throw new ArgumentNullException(nameof(mergeFrom));
			
			// 1) merge json
			// 2) copy merge from binary data to merge to binary data
			// 3) Fix up buffer views to be the new offset
			int previousBufferViewsCount = mergeTo.Root.BufferViews?.Count ?? 0;
			long previousBufferSize = mergeTo.BinaryChunkInfo.Length;
			GLTFHelpers.MergeGLTF(mergeTo.Root, mergeFrom.Root);
			if (mergeFrom.BinaryChunkInfo.Length > 0)
			{
				_AddBinaryData(mergeTo, mergeFrom.Stream, false, mergeFrom.BinaryChunkInfo.StartPosition + mergeFrom.Header.GetChunkHeaderSize(), mergeFrom.BinaryChunkInfo.Length);
			}

			if (mergeTo.Root.BufferViews != null)
			{
				for (int i = previousBufferViewsCount; i < mergeTo.Root.BufferViews.Count; ++i)
				{
					// the appended binary data starts after the previous binary data
					mergeTo.Root.BufferViews[i].ByteOffset += previousBufferSize;
				}
			}
		}

		/// <summary>
		/// Removes a blob from the GLB at the given BufferView
		/// Updates accessors and images to have correct new bufferview index
		/// This function can invalidate BufferViewId's returned by previous function
		/// </summary>
		/// <param name="glb">The glb to remove from</param>
		/// <param name="bufferViewId">The buffer to remove</param>
		public static void RemoveBinaryData(GLBObject glb, BufferViewId bufferViewId)
		{
			if (glb == null) throw new ArgumentNullException(nameof(glb));
			if (bufferViewId == null) throw new ArgumentNullException(nameof(bufferViewId));

			BufferView bufferViewToRemove = bufferViewId.Value;
			int id = bufferViewId.Id;
			if (bufferViewToRemove.ByteOffset + bufferViewToRemove.ByteLength == glb.BinaryChunkInfo.Length)
			{
				long bufferViewLength = bufferViewToRemove.ByteLength;
				glb.SetFileLength(glb.Header.FileLength - bufferViewLength);
				glb.SetBinaryChunkLength(glb.BinaryChunkInfo.Length - bufferViewLength);
				if (glb.BinaryChunkInfo.Length == 0)
				{
					glb.Root.Buffers.RemoveAt(0);
					foreach (BufferView bufferView in glb.Root.BufferViews) // other buffers may still exist, and their index has now changed
					{
						--bufferView.Buffer.Id;
					}

					glb.SetFileLength(glb.Header.FileLength - glb.Header.GetChunkHeaderSize());
				}
				else
				{
					glb.Root.Buffers[0].ByteLength = glb.BinaryChunkInfo.Length;

					// write binary chunk header
					WriteChunkHeader(glb.Stream, glb.Header, glb.BinaryChunkInfo);
				}

				// trim the end
				glb.Stream.SetLength(glb.StreamStartPosition + glb.Header.FileLength);

				// write glb header
				WriteHeader(glb.Stream, glb.Header, glb.StreamStartPosition);
			}

			glb.Root.BufferViews.RemoveAt(id);
			if (glb.Root.Accessors != null)
			{
				foreach (Accessor accessor in glb.Root.Accessors) // shift over all accessors
				{
					if (accessor.BufferView != null && accessor.BufferView.Id >= id)
					{
						--accessor.BufferView.Id;
					}

					if (accessor.Sparse != null)
					{
						if (accessor.Sparse.Indices?.BufferView.Id >= id)
						{
							--accessor.Sparse.Indices.BufferView.Id;
						}

						if (accessor.Sparse.Values?.BufferView.Id >= id)
						{
							--accessor.Sparse.Values.BufferView.Id;
						}
					}
				}
			}

			if (glb.Root.Images != null)
			{
				foreach (GLTFImage image in glb.Root.Images)
				{
					if (image.BufferView != null && image.BufferView.Id >= id)
					{
						--image.BufferView.Id;
					}
				}
			}
		}

		/// <summary>
		/// Added function to set the root
		/// </summary>
		/// <param name="glb">GLB to add</param>
		/// <param name="newRoot">The new root to update it with</param>
		public static void SetRoot(GLBObject glb, GLTFRoot newRoot)
		{
			if (newRoot != null)
			{
				glb.Root = newRoot;
			}
		}

		/// <summary>
		/// Converts the GLB to the given binary format version, moving the chunks to the header sizes and alignment of that version.
		/// </summary>
		/// <param name="glb">The glb to convert</param>
		/// <param name="glbVersion">The GLB binary format version, 2 or 3</param>
		public static void ConvertVersion(GLBObject glb, uint glbVersion)
		{
			if (glb == null) throw new ArgumentNullException(nameof(glb));
			if (glbVersion != 2 && glbVersion != 3) throw new ArgumentOutOfRangeException(nameof(glbVersion), "Only GLB versions 2 and 3 are supported.");
			if (glb.Header.Version == glbVersion) return;

			Relayout(glb, glbVersion, glb.JsonChunkInfo.Length);
			UpdateStream(glb);
		}

		/// <summary>
		/// Moves the chunks to make room for a JSON chunk of the given length, using the header sizes and alignment of the given version.
		/// Moves the binary chunk data and rewrites all headers. The JSON chunk content has to be written again afterwards.
		/// </summary>
		private static void Relayout(GLBObject glb, uint glbVersion, long jsonChunkLength)
		{
			GLBHeader oldHeader = glb.Header;
			GLBChunkInfo oldBinaryChunkInfo = glb.BinaryChunkInfo;
			GLBHeader glbHeader = new GLBHeader { Version = glbVersion };
			long chunkHeaderSize = glbHeader.GetChunkHeaderSize();

			jsonChunkLength = CalculateAlignment(jsonChunkLength, glbHeader.GetAlignment());
			long binaryDataLength = oldBinaryChunkInfo.Length;
			long binaryChunkLength = CalculateAlignment(binaryDataLength, glbHeader.GetAlignment());
			long jsonChunkStartPosition = glb.StreamStartPosition + glbHeader.GetFileHeaderSize();
			long binaryChunkStartPosition = jsonChunkStartPosition + chunkHeaderSize + jsonChunkLength;
			long fileEndPosition = binaryChunkStartPosition + (binaryDataLength > 0 ? chunkHeaderSize + binaryChunkLength : 0);
			long fileLength = fileEndPosition - glb.StreamStartPosition;

			ThrowIfTooLarge(glbVersion, fileLength);

			if (fileEndPosition > glb.Stream.Length)
			{
				glb.Stream.SetLength(fileEndPosition);
			}

			if (binaryDataLength > 0)
			{
				long oldBinaryDataStartPosition = oldBinaryChunkInfo.StartPosition + oldHeader.GetChunkHeaderSize();
				long binaryDataStartPosition = binaryChunkStartPosition + chunkHeaderSize;
				MoveData(glb.Stream, oldBinaryDataStartPosition, binaryDataStartPosition, binaryDataLength);

				// zero the padding, which may contain old data
				glb.Stream.Position = binaryDataStartPosition + binaryDataLength;
				byte[] padding = new byte[binaryChunkLength - binaryDataLength];
				glb.Stream.Write(padding, 0, padding.Length);
			}

			glb.Stream.SetLength(fileEndPosition);
			glbHeader.FileLength = fileLength;
			glb.Header = glbHeader;
			glb.JsonChunkInfo = new GLBChunkInfo
			{
				StartPosition = jsonChunkStartPosition,
				Length = jsonChunkLength,
				Type = GLBChunkFormat.JSON
			};
			glb.BinaryChunkInfo = new GLBChunkInfo
			{
				StartPosition = binaryChunkStartPosition,
				Type = GLBChunkFormat.BIN
			};

			WriteHeader(glb.Stream, glb.Header, glb.StreamStartPosition);
			WriteChunkHeader(glb.Stream, glb.Header, glb.JsonChunkInfo);
			if (binaryDataLength > 0)
			{
				glb.SetBinaryChunkLength(binaryChunkLength);
				WriteChunkHeader(glb.Stream, glb.Header, glb.BinaryChunkInfo);
			}
		}

		internal static void ThrowIfTooLarge(uint glbVersion, long fileLength)
		{
			// The file length field of version 2 is 32-bit. Version 3 allows up to 2^63-1 bytes, which fits in a long.
			if (glbVersion == 2 && fileLength > uint.MaxValue)
			{
				throw new InvalidOperationException("GLB version 2 has exceeded max allowed size (4 GB)");
			}
		}

		private static void MoveData(Stream stream, long sourcePosition, long destinationPosition, long length)
		{
			if (destinationPosition > sourcePosition)
			{
				// copies from the end, so overlapping ranges are not overwritten before they are read
				stream.Position = sourcePosition;
				stream.CopyToSelf(destinationPosition, length);
				return;
			}

			// copies from the start, which is safe when moving data towards the start of the stream
			CopyData(stream, sourcePosition, stream, destinationPosition, length);
		}

		private static void CopyData(Stream source, long sourcePosition, Stream destination, long destinationPosition, long length)
		{
			byte[] buffer = new byte[81920];
			for (long offset = 0; offset < length;)
			{
				source.Position = sourcePosition + offset;
				int read = source.Read(buffer, 0, (int)System.Math.Min(buffer.Length, length - offset));
				if (read == 0) throw new EndOfStreamException();
				destination.Position = destinationPosition + offset;
				destination.Write(buffer, 0, read);
				offset += read;
			}
		}

		private static void WriteHeader(Stream stream, GLBHeader glbHeader, long streamStartPosition)
		{
			stream.Position = streamStartPosition;
			byte[] magicNumber = BitConverter.GetBytes(GLBHeader.GLTF_MAGIC_NUMBER);
			byte[] version = BitConverter.GetBytes(glbHeader.Version);
			byte[] length;
			if (glbHeader.Version == 2)
			{
				length = BitConverter.GetBytes((uint)glbHeader.FileLength);
			}
			else
			{
				length = BitConverter.GetBytes((ulong)glbHeader.FileLength);
			}

			stream.Write(magicNumber, 0, magicNumber.Length);
			stream.Write(version, 0, version.Length);
			stream.Write(length, 0, length.Length);
		}

		private static void WriteChunkHeader(Stream stream, GLBHeader glbHeader, GLBChunkInfo chunkInfo)
		{
			stream.Position = chunkInfo.StartPosition;
			byte[] typeBytes = BitConverter.GetBytes((uint)chunkInfo.Type);
			// Binary format version 2 and 3 have different chunk header sizes and field order.
			if (glbHeader.Version == 2)
			{
				byte[] lengthBytes = BitConverter.GetBytes((uint)chunkInfo.Length);
				stream.Write(lengthBytes, 0, lengthBytes.Length);
				stream.Write(typeBytes, 0, typeBytes.Length);
			}
			else
			{
				byte[] encodingBytes = BitConverter.GetBytes(chunkInfo.Encoding);
				byte[] lengthBytes = BitConverter.GetBytes((ulong)chunkInfo.Length);
				stream.Write(typeBytes, 0, typeBytes.Length);
				stream.Write(encodingBytes, 0, encodingBytes.Length);
				stream.Write(lengthBytes, 0, lengthBytes.Length);
			}
		}

		public static long CalculateAlignment(long currentSize, long byteAlignment)
		{
			return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
		}
	}
}
