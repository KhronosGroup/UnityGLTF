using System.Collections.Generic;

namespace AssetGenerator
{
    internal class Manifest
    {
        public string Folder;
        public List<Model> Models = new List<Model>();

        // Model group, to be listed in the manifest as the folder name
        public Manifest()
        {
            
        }

        // Model properties to be listed in the manifest
        public class Model
        {
            public string FileName;
            [Newtonsoft.Json.JsonProperty( NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore )]
            public string SampleImageName;
            public Camera Camera;

            public Model()
            {
                
            }
        }

        // Camera properties
        public class Camera
        {
            public float[] Translation = new float[3];

            public Camera()
            {
                //cameratranslation.CopyTo(Translation);
            }
        }
    }
}
