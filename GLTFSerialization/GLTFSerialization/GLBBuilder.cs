using GLTF;
using GLTF.Schema;
using GLTF.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Buffer = GLTF.Schema.Buffer;

namespace UnityGLTF
{
	// Object representing our representation of a GLB
	public class GLBObject
	{
		public GLTFRoot Root;					// Parsed JSON of the GLB
		public Stream Stream;					// Read/Write Stream that GLB exists in
		public GLBHeader Header;				// Header of GLB
		public long StreamStartPosition;		// Start position of stream
		public ChunkInfo JsonChunkInfo;			// Information on JSON chunk
		public ChunkInfo BinaryChunkInfo;		// Information on Binary chunk
	}
	
	// Static class for construction GLB objects. These API's only work with the .NET 4.6 runtime and above.
	public static class GLBBuilder
	{
		/// <summary>
		/// Turns a glTF file w/ structure into a GLB. Does not currently copy binary data
		/// </summary>
		/// <param name="root">The glTF root to turn into a GLBObject</param>
		/// <param name="glbOutStream">Output stream to write the GLB to</param>
		/// <param name="loader">Loader for loading external components from GLTFRoot. The loader will receive uris and return the stream to the resource</param>
		/// <returns>A constructed GLBObject</returns>
		private static GLBObject ConstructFromGLTF(GLTFRoot root, Stream glbOutStream, Func<string, Stream> loader)
		{
			if (root == null) throw new ArgumentNullException(nameof(root));
			if (glbOutStream == null) throw new ArgumentNullException(nameof(glbOutStream));
			
			MemoryStream gltfJsonStream = new MemoryStream();
			using (StreamWriter sw = new StreamWriter(gltfJsonStream))
			{
				root.Serialize(sw, true);
				sw.Flush();

				long proposedLength = gltfJsonStream.Length + GLTFParser.HEADER_SIZE + GLTFParser.CHUNK_HEADER_SIZE;
				if (gltfJsonStream.Length > uint.MaxValue)
				{
					throw new ArgumentException("Serialized root cannot exceed uint.maxvalue", nameof(root));
				}
				uint proposedLengthAsUint = (uint)proposedLength;
				glbOutStream.SetLength(proposedLengthAsUint);
				GLBObject glbObject = new GLBObject
				{
					Header = new GLBHeader
					{
						FileLength = proposedLengthAsUint,
						Version = 2
					},
					Root = root,
					Stream = glbOutStream,
					JsonChunkInfo = new ChunkInfo
					{
						Length = (uint)gltfJsonStream.Length,
						StartPosition = GLTFParser.HEADER_SIZE,
						Type = ChunkFormat.JSON
					}
				};

				// write header
				glbOutStream.Position = 0;
				WriteHeader(glbOutStream, glbObject.Header);

				// write chunk header
				WriteChunkHeader(glbOutStream, glbObject.JsonChunkInfo);
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
				inputGLBStream.Position = 0;
				inputGLBStream.CopyTo(outStream);
			}
			else
			{
				outStream = inputGLBStream;
			}

			// header information is 4 bytes in, past the magic number
			inputGLBStream.Position = 4 + inputGLBStreamStartPosition;
			GLBHeader header = GLTFParser.ParseGLBHeader(inputGLBStream);

			inputGLBStream.Position = GLTFParser.HEADER_SIZE + inputGLBStreamStartPosition;
			List<ChunkInfo> allChunks = GLTFParser.FindChunks(inputGLBStream);
			ChunkInfo jsonChunkInfo = new ChunkInfo
			{
				Type = ChunkFormat.JSON
			};
			ChunkInfo binaryChunkInfo = new ChunkInfo
			{
				Type = ChunkFormat.BIN
			};

			foreach (ChunkInfo chunkInfo in allChunks)
			{
				switch (chunkInfo.Type)
				{
					case ChunkFormat.JSON:
						jsonChunkInfo = chunkInfo;
						break;
					case ChunkFormat.BIN:
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
				Header = header,
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
			long streamStartPosition = 0)
		{
			if (inStream == null) throw new ArgumentNullException(nameof(inStream));
			inStream.Position = streamStartPosition;
			
			GLTFRoot root = GLTFParser.ParseJson(inStream, streamStartPosition);
			if (!root.IsGLB)
			{
				return ConstructFromGLTF(root, glbOutStream, loader);
			}

			return ConstructFromGLB(root, inStream, glbOutStream, streamStartPosition);
		}

		/// <summary>
		/// Saves out the GLBObject to its own stream
		/// The GLBObject stream will be updated to be the output stream. Callers are reponsible for handling Stream lifetime
		/// </summary>
		/// <param name="glb">The GLB to flush to the output stream and update</param>
		/// <returns>A GLBObject that is based upon outStream</returns>
		public static void UpdateStream(GLBObject glb)
		{
			if (glb.Root == null) throw new ArgumentException("glb Root property cannot be null", nameof(glb.Root));
			if (glb.Stream == null) throw new ArgumentException("glb GLBStream property cannot be null", nameof(glb.Stream));

			MemoryStream gltfJsonStream = new MemoryStream();
			using (StreamWriter sw = new StreamWriter(gltfJsonStream))
			{
				glb.Root.Serialize(sw, true);   // todo: this could out of memory exception
				sw.Flush();

				if (gltfJsonStream.Length > int.MaxValue)
				{
					// todo: make this a non generic exception
					throw new Exception("JSON chunk of GLB has exceeded maximum allowed size (4 GB)");
				}

				// realloc of out of space
				if (glb.JsonChunkInfo.Length < gltfJsonStream.Length)
				{
					uint proposedJsonChunkLength = (uint)Math.Min((long)gltfJsonStream.Length * 2, uint.MaxValue); // allocate double what is required
					proposedJsonChunkLength = CalculateAlignment(proposedJsonChunkLength, 4);

					// chunks must be 4 byte aligned
					uint amountToAddToFile = proposedJsonChunkLength - glb.JsonChunkInfo.Length;

					// new proposed length = propsoedJsonBufferSize - currentJsonBufferSize + totalFileLength
					long proposedLength = amountToAddToFile + glb.Header.FileLength;
					if (proposedLength > uint.MaxValue)
					{
						throw new Exception("GLB has exceeded max allowed size (4 GB)");
					}

					uint proposedLengthAsUint = (uint)proposedLength;

					try
					{
						glb.Stream.SetLength(proposedLength);
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

					long newBinaryChunkStartPosition =
						GLTFParser.HEADER_SIZE + GLTFParser.CHUNK_HEADER_SIZE + proposedJsonChunkLength;

					// Copy the binary chunk to new position
					glb.Stream.Position = glb.BinaryChunkInfo.StartPosition;
					glb.BinaryChunkInfo.StartPosition = newBinaryChunkStartPosition;
					uint lengthToCopy = glb.BinaryChunkInfo.Length + GLTFParser.CHUNK_HEADER_SIZE;
					glb.Stream.CopyToSelf((int)newBinaryChunkStartPosition, lengthToCopy);  // todo: we need to be able to copy while doing it with smaller buffers. Also int is smaller than uint, so this is not standards compliant.

					// write out new GLB length
					glb.Header.FileLength = proposedLengthAsUint;
					glb.Stream.Position = 0; // length start position
					WriteHeader(glb.Stream, glb.Header);

					// write out new JSON header
					glb.JsonChunkInfo.Length = proposedJsonChunkLength;
					WriteChunkHeader(glb.Stream, glb.JsonChunkInfo);
				}

				// clear the buffer
				glb.Stream.Position = glb.JsonChunkInfo.StartPosition + GLTFParser.CHUNK_HEADER_SIZE;
				uint amountToCopy = glb.JsonChunkInfo.Length;
				while (amountToCopy != 0)
				{
					int currAmountToCopy = (int)Math.Min(amountToCopy, int.MaxValue);
					byte[] filler =
						Encoding.ASCII.GetBytes(new string(' ', currAmountToCopy));
					glb.Stream.Write(filler, 0, filler.Length);
					amountToCopy -= (uint)currAmountToCopy;
				}

				// write new JSON data
				gltfJsonStream.Position = 0;
				glb.Stream.Position = glb.JsonChunkInfo.StartPosition + GLTFParser.CHUNK_HEADER_SIZE;
				gltfJsonStream.CopyTo(glb.Stream);
				glb.Stream.Flush();
			}
		}

		/// <summary>
		/// Adds binary data to the GLB
		/// </summary>
		/// <param name="glb">The glb to update</param>
		/// <param name="binaryData">The binary data to append</param>
		/// <param name="streamStartPosition">Start position of stream</param>
		/// <returns>The location of the added buffer view</returns>
		public static BufferViewId AddBinaryData(GLBObject glb, Stream binaryData, uint streamStartPosition = 0)
		{
			if(glb.Stream == null) throw new ArgumentException("glb Stream cannot be null", nameof(glb));
			if(binaryData == null) throw new ArgumentNullException(nameof(binaryData));
			if(binaryData.Length > uint.MaxValue) throw new ArgumentException("Stream cannot be larger than uint.MaxValue", nameof(binaryData));

			return _AddBinaryData(glb, binaryData, true, streamStartPosition);
		}

		private static BufferViewId _AddBinaryData(GLBObject glb, Stream binaryData, bool createBufferView, long streamStartPosition)
		{
			binaryData.Position = streamStartPosition;

			// Append new binary chunk to end
			uint blobLengthAsUInt = (uint)(CalculateAlignment((uint) binaryData.Length, 4) - streamStartPosition);
			uint newBinaryBufferSize = glb.BinaryChunkInfo.Length + blobLengthAsUInt;
			uint newGLBSize = glb.Header.FileLength + blobLengthAsUInt;
			uint blobWritePosition = glb.Header.FileLength;
			glb.Stream.SetLength(glb.Header.FileLength + blobLengthAsUInt);
			glb.Stream.Position = blobWritePosition;	// assuming the end of the file is the end of the binary chunk
			binaryData.CopyTo(glb.Stream);							// make sure this doesn't supersize it
			glb.Header.FileLength = newGLBSize;
			glb.BinaryChunkInfo.Length = newBinaryBufferSize;
			if (glb.Root.Buffers == null)
			{
				glb.Root.Buffers = new List<Buffer>();
			}

			if (glb.Root.Buffers.Count == 0)
			{
				glb.Root.Buffers.Add(new Buffer());
			}

			glb.Root.Buffers[0].ByteLength = newBinaryBufferSize;

			// write glb header past magic number
			glb.Stream.Position = 0;
			WriteHeader(glb.Stream, glb.Header);

			// write binary chunk header
			glb.Stream.Position = glb.BinaryChunkInfo.StartPosition;
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
					ByteLength = blobLengthAsUInt, // figure out whether glb size is wrong or if documentation is unclear
					ByteOffset = glb.BinaryChunkInfo.Length - blobLengthAsUInt
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
			// 2) copy mergefrom binary data to mergeto binary data
			// 3) Fix up bufferviews to be the new offset
			int previousBufferViewsCount = mergeTo.Root.BufferViews?.Count ?? 0;
			uint previousBufferSize = mergeTo.BinaryChunkInfo.Length;
			GLTFHelpers.MergeGLTF(mergeTo.Root, mergeFrom.Root);
			_AddBinaryData(mergeTo, mergeFrom.Stream, false, mergeFrom.BinaryChunkInfo.StartPosition + GLTFParser.CHUNK_HEADER_SIZE);
			uint bufferSizeDiff =
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
				uint bufferViewLengthAsUint = bufferViewToRemove.ByteLength;
				glb.Header.FileLength -= bufferViewLengthAsUint;
				glb.BinaryChunkInfo.Length -= bufferViewLengthAsUint;
				if (glb.BinaryChunkInfo.Length == 0)
				{
					glb.Root.Buffers.RemoveAt(0);
					foreach (BufferView bufferView in glb.Root.BufferViews) // other buffers may still exist, and their index has now changed
					{
						--bufferView.Buffer.Id;
					}

					glb.Header.FileLength -= GLTFParser.CHUNK_HEADER_SIZE;
				}
				else
				{
					glb.Root.Buffers[0].ByteLength = glb.BinaryChunkInfo.Length;

					// write binary chunk header
					glb.StreamStartPosition = glb.BinaryChunkInfo.StartPosition;
					WriteChunkHeader(glb.Stream, glb.BinaryChunkInfo);
				}

				// trim the end
				glb.Stream.SetLength(glb.Header.FileLength);

				// write glb header
				glb.StreamStartPosition = 0;
				WriteHeader(glb.Stream, glb.Header);
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
				foreach (Image image in glb.Root.Images)
				{
					if (image.BufferView != null && image.BufferView.Id >= id)
					{
						--image.BufferView.Id;
					}
				}
			}
		}
		
		private static void WriteHeader(Stream stream, GLBHeader header)
		{
			byte[] magicNumber = BitConverter.GetBytes(GLTFParser.MAGIC_NUMBER);
			byte[] version = BitConverter.GetBytes(header.Version);
			byte[] length = BitConverter.GetBytes(header.FileLength);

			stream.Write(magicNumber, 0, magicNumber.Length);
			stream.Write(version, 0, version.Length);
			stream.Write(length, 0, length.Length);
		}

		private static void WriteChunkHeader(Stream stream, ChunkInfo chunkInfo)
		{
			stream.Position = chunkInfo.StartPosition;
			byte[] lengthBytes = BitConverter.GetBytes(chunkInfo.Length);
			byte[] typeBytes = BitConverter.GetBytes((uint)chunkInfo.Type);

			stream.Write(lengthBytes, 0, lengthBytes.Length);
			stream.Write(typeBytes, 0, lengthBytes.Length);
		}

		public static uint CalculateAlignment(uint currentSize, uint byteAlignment)
		{
			return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
		}
	}
}
