using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GLTF.Utilities
{
	internal static class JsonReaderExtensions
	{
		public static uint ReadDoubleAsUInt32(this JsonReader reader)
		{
			return  (uint)System.Math.Round(reader.ReadAsDouble().Value);
		}
	}
}
