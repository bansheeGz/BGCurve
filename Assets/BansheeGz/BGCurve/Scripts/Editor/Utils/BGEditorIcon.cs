using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BansheeGz.BGSpline.Editor
{
	public class BGEditorIcon 
	{
		public const string ResourcesGuid = "24c6ad7c8291acc41914cdd6572f2f54";

		private static byte[] data;
		private static bool loadTried;

		private readonly int width;
		private readonly int height;
		private Texture2D texture;

		
		private readonly int offset;
		private readonly int length;

		public int Offset
		{
			get { return offset; }
		}

		public int Length
		{
			get { return length; }
		}

		public Texture2D Texture
		{
			get
			{
				if (texture != null) return texture;
                
                
				var data = Data;
				if (data == null) throw new Exception("Can not access data stream to load resource: stream is null");
				if (data.Length < Offset + Length) throw new Exception("Can not read resource from stream: not enough data: " + data.Length + "<" + (Offset + Length));

				texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
/*
                //this wont compile without unsafe switch for compiler
                unsafe
                {
                    fixed (byte* p = data)
                    {
                        var ptr = new IntPtr(((IntPtr) p).ToInt64() + sizeof(byte) * Offset);
                        texture.LoadRawTextureData(ptr, Length);
                    }
                }
*/

				//this is probably much slower than using  unsafe code above
				var textureData = new byte[Length];
				Buffer.BlockCopy(data, Offset, textureData, 0, Length);
				texture.LoadRawTextureData(textureData);
                    
				texture.Apply();
                    
				Object.DontDestroyOnLoad(texture);
				texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

				return texture;
			}
		}

		private byte[] Data
		{
			get
			{
				if (data != null || loadTried) return data;

				loadTried = true;
				var path = AssetDatabase.GUIDToAssetPath(ResourcesGuid);
				if (path == null) throw new Exception("Can not resolve icons resource asset with GUID: " + ResourcesGuid);
				data = Unzip(File.ReadAllBytes(path));

				return data;
			}
		}

		public BGEditorIcon(int offset, int length, int width, int height) 
		{
			this.offset = offset;
			this.length = length;
			this.width = width;
			this.height = height;
		}


		public static implicit operator Texture2D(BGEditorIcon icon)
		{
			return icon.Texture;
		}
		
		private static byte[] Unzip(byte[] input)
		{
			using (var deflateStream = new DeflateStream(new MemoryStream(input), CompressionMode.Decompress))
			{
				using (var outputStream = new MemoryStream())
				{
					CopyTo(deflateStream, outputStream);
					return outputStream.ToArray();
				}
			}
		}

		private  static void CopyTo(Stream input, Stream output)
		{
			var buffer = new byte[64 * 1024];
			int bytesRead;
			while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, bytesRead);
		}
	}
}