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
		private static GLBObject ConstructFromGLTF(GLTFRoot root, Stream glbOutStream, Func<string, Stream> loader, int glbVersion)
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
					Version = (uint)(glbVersion == -1 ? 2 : glbVersion)
				};
				long proposedFileLength = gltfJsonStream.Length + glbHeader.GetFileHeaderSize() + glbHeader.GetChunkHeaderSize();
				// If automatic, and the file length would be too big for GLB version 2, automatically upgrade to GLB version 3.
				if (glbVersion == -1 && proposedFileLength > uint.MaxValue)
				{
					glbHeader.Version = 3;
					proposedFileLength = gltfJsonStream.Length + glbHeader.GetFileHeaderSize() + glbHeader.GetChunkHeaderSize();
				}
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
				WriteChunkHeader(glbOutStream, glbObject.JsonChunkInfo);

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
			if (outStream != null)
			{
				inputGLBStream.Position = inputGLBStreamStartPosition;
				inputGLBStream.CopyTo(outStream);
			}
			else
			{
				outStream = inputGLBStream;
			}

			// header information is 4 bytes in, past the magic number
			inputGLBStream.Position = 4 + inputGLBStreamStartPosition;
			GLBHeader glbHeader = GLTFParser.ParseGLBHeader(inputGLBStream);

			inputGLBStream.Position = glbHeader.GetFileHeaderSize() + inputGLBStreamStartPosition;
			List<GLBChunkInfo> allChunks = GLTFParser.FindChunks(inputGLBStream);
			GLBChunkInfo jsonChunkInfo = new GLBChunkInfo
			{
				Type = GLBChunkFormat.JSON
			};
			GLBChunkInfo binaryChunkInfo = new GLBChunkInfo
			{
				Type = GLBChunkFormat.BIN
			};

			foreach (GLBChunkInfo chunkInfo in allChunks)
			{
				switch (chunkInfo.Type)
				{
					case GLBChunkFormat.JSON:
						jsonChunkInfo = chunkInfo;
						break;
					case GLBChunkFormat.BIN:
						binaryChunkInfo = chunkInfo;
						break;
				}
			}

			if (jsonChunkInfo.Length == 0)
			{
				throw new ArgumentException("JSON chunk must exists for valid GLB", nameof(inputGLBStream));
			}

			// todo: compute the initial bufferview list
			return new GLBObject
			{
				Root = root,
				Stream = outStream,
				StreamStartPosition = inputGLBStreamStartPosition,
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

			if (inStream.Length > 0)
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
					return ConstructFromGLTF(root, glbOutStream, loader, 2);
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
					StartPosition = glbHeader.GetFileHeaderSize(),
					Type = GLBChunkFormat.JSON,
					Encoding = 0
				},
				BinaryChunkInfo = new GLBChunkInfo
				{
					Length = 0,
					StartPosition = glbHeader.GetFileHeaderSize() + glbHeader.GetChunkHeaderSize(),
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
				GLBHeader glbHeader = glb.Header; // Be sure to write this back in if it was updated.
				// If the file length would be too big for GLB version 2, automatically upgrade to GLB version 3.
				if (glbHeader.Version == 2 && gltfJsonStream.Length > uint.MaxValue)
				{
					glbHeader.Version = 3;
					glb.Header = glbHeader;
				}
				long fileHeaderSize = glbHeader.GetFileHeaderSize();
				long chunkHeaderSize = glbHeader.GetChunkHeaderSize();

				// realloc of out of space
				if (glb.JsonChunkInfo.Length < gltfJsonStream.Length)
				{
					long proposedJsonChunkLength = System.Math.Min((long)gltfJsonStream.Length * 2, (long)uint.MaxValue); // allocate double what is required
					proposedJsonChunkLength = CalculateAlignment(proposedJsonChunkLength, 4);
					
					// chunks must be 4 byte aligned
					long amountToAddToFile = proposedJsonChunkLength - glb.JsonChunkInfo.Length;

					// we have not yet initialized a json chunk before
					if (glb.JsonChunkInfo.Length == 0)
					{
						amountToAddToFile += fileHeaderSize;
						glb.SetJsonChunkStartPosition(chunkHeaderSize);
 					}

					long proposedFileLength = amountToAddToFile + glbHeader.FileLength;
					// If the file length would be too big for GLB version 2, automatically upgrade to GLB version 3.
					if (glbHeader.Version == 2 && proposedFileLength > uint.MaxValue)
					{
						amountToAddToFile -= fileHeaderSize;
						glbHeader.Version = 3;
						glb.Header = glbHeader;
						fileHeaderSize = glbHeader.GetFileHeaderSize();
						chunkHeaderSize = glbHeader.GetChunkHeaderSize();
						amountToAddToFile += fileHeaderSize;
						glb.SetJsonChunkStartPosition(chunkHeaderSize);
					}

					try
					{
						glb.Stream.SetLength(proposedFileLength);
					}
					catch (IOException e)
					{
#if WINDOWS_UWP
						Debug.WriteLine(e);
#else
						Console.WriteLine(e);
#endif
						throw;
					}

					long newBinaryChunkDataStartPosition = fileHeaderSize + chunkHeaderSize + proposedJsonChunkLength;
				
					glb.Stream.Position = glb.BinaryChunkInfo.StartPosition;
					glb.SetBinaryChunkStartPosition(newBinaryChunkDataStartPosition);
					if (glb.BinaryChunkInfo.Length > 0)
					{
						long lengthToCopy = glb.BinaryChunkInfo.Length + chunkHeaderSize;

						// todo: we need to be able to copy while doing it with smaller buffers. Also int is smaller than uint, so this is not standards compliant.
						glb.Stream.CopyToSelf((int)newBinaryChunkDataStartPosition,
							lengthToCopy);
					}

					// write out new GLB length
					glb.SetFileLength(proposedFileLength);
					WriteHeader(glb.Stream, glbHeader, glb.StreamStartPosition);

					// write out new JSON header
					glb.SetJsonChunkLength(proposedJsonChunkLength);
					WriteChunkHeader(glb.Stream, glb.JsonChunkInfo);
				}

				// clear the buffer
				glb.Stream.Position = glb.JsonChunkInfo.StartPosition + chunkHeaderSize;
				long amountToCopy = glb.JsonChunkInfo.Length;
				while (amountToCopy != 0)
				{
					long currAmountToCopy = System.Math.Min(amountToCopy, (long)int.MaxValue);
					byte[] filler =
						Encoding.ASCII.GetBytes(new string(' ', (int)currAmountToCopy));
					glb.Stream.Write(filler, 0, filler.Length);
					amountToCopy -= (uint)currAmountToCopy;
				}

				// write new JSON data
				gltfJsonStream.Position = 0;
				glb.Stream.Position = glb.JsonChunkInfo.StartPosition + chunkHeaderSize;
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
			if (glb.Header.Version == 2 && binaryData.Length > uint.MaxValue) throw new ArgumentException("Stream cannot be larger than uint.MaxValue", nameof(binaryData));

			return _AddBinaryData(glb, binaryData, createBufferView, streamStartPosition, bufferViewName);
		}

		private static BufferViewId _AddBinaryData(GLBObject glb, Stream binaryData, bool createBufferView, long streamStartPosition, string bufferViewName = null)
		{
			binaryData.Position = streamStartPosition;

			// Append new binary chunk to end
			long blobLength = CalculateAlignment(binaryData.Length - streamStartPosition, 4);
			long newBinaryBufferSize = glb.BinaryChunkInfo.Length + blobLength;
			long newGLBSize = glb.Header.FileLength + blobLength;
			long blobWritePosition = glb.Header.FileLength;
			long chunkHeaderSize = glb.Header.GetChunkHeaderSize();

			// there was an existing file that had no binary chunk info previously
			if (glb.BinaryChunkInfo.Length == 0)
			{
				newGLBSize += chunkHeaderSize;
				blobWritePosition += chunkHeaderSize;
				glb.SetBinaryChunkStartPosition(glb.Header.FileLength);  // if 0, then appends chunk info at the end
			}

			glb.Stream.SetLength(glb.Header.FileLength + blobLength);
			glb.Stream.Position = blobWritePosition;    // assuming the end of the file is the end of the binary chunk
			binaryData.CopyTo(glb.Stream);              // make sure this doesn't supersize it

			glb.SetFileLength(newGLBSize);
			glb.SetBinaryChunkLength(newBinaryBufferSize);

			// write glb header past magic number
			WriteHeader(glb.Stream, glb.Header, glb.StreamStartPosition);
			
			WriteChunkHeader(glb.Stream, glb.BinaryChunkInfo);

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
			long toChunkHeaderSize = mergeTo.Header.GetChunkHeaderSize();
			_AddBinaryData(mergeTo, mergeFrom.Stream, false, mergeFrom.BinaryChunkInfo.StartPosition + toChunkHeaderSize);
			long bufferSizeDiff =
				mergeTo.BinaryChunkInfo.Length -
				previousBufferSize; // calculate buffer size change to update the byte offsets of the appended buffer views

			if (mergeTo.Root.BufferViews != null)
			{
				for (int i = previousBufferViewsCount; i < mergeTo.Root.BufferViews.Count; ++i)
				{
					mergeTo.Root.BufferViews[i].ByteOffset += bufferSizeDiff;
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
					WriteChunkHeader(glb.Stream, glb.BinaryChunkInfo);
				}

				// trim the end
				glb.Stream.SetLength(glb.Header.FileLength);

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

		private static void WriteChunkHeader(Stream stream, GLBChunkInfo chunkInfo)
		{
			stream.Position = chunkInfo.StartPosition;
			byte[] lengthBytes = BitConverter.GetBytes(chunkInfo.Length);
			byte[] typeBytes = BitConverter.GetBytes((uint)chunkInfo.Type);

			stream.Write(lengthBytes, 0, lengthBytes.Length);
			stream.Write(typeBytes, 0, lengthBytes.Length);
		}

		public static long CalculateAlignment(long currentSize, long byteAlignment)
		{
			return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
		}
	}
}
