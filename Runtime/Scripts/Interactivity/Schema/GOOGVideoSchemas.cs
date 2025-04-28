
using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
    
    [Serializable]
    public class GOOG_VideoData {//: GLTFChildOfRootProperty {
        public int source;
        public int playhead;
        public bool autoPlay;
        public bool loop;


        public JObject SerializeObject()
        {
            JObject jo = new JObject();

            jo.Add(nameof(source), source);
            jo.Add(nameof(playhead), playhead);
            jo.Add(nameof(autoPlay), autoPlay);
            jo.Add(nameof(loop), loop);

            return jo;
        }
    }

    [Serializable]
    public class GOOG_VideoSource : GLTFChildOfRootProperty {

        public string uri;
        public string mimeType;
        public BufferViewId bufferView;

        public JObject Serialize() 
        {
            var jo = new JObject();

            if (!string.IsNullOrEmpty(uri))
            {
                jo.Add(nameof(uri), uri);
            }
            else
            {
                if (!string.IsNullOrEmpty(mimeType))
                    jo.Add(nameof(mimeType), mimeType);
                jo.Add(nameof(bufferView), bufferView.Id);
            }

            return jo;
        }
    }
    [Serializable]
    public class GOOG_Video_Data : IExtension
    {
        public const string ExtensionName = "GOOG_video";

        public List<GOOG_VideoData> videoDatas;

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            if (videoDatas != null && videoDatas.Count > 0)
            {
                JArray videoArr = new JArray();

                foreach (var videoData in videoDatas)
                {
                    videoArr.Add(videoData.SerializeObject());
                }

                jo.Add(new JProperty(nameof(videoDatas), videoArr));
            }

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new GOOG_Video_Data()
            {
                videoDatas = videoDatas
            };
        }
    }

    [Serializable]
    public class GOOG_Video : IExtension
    {
        public const string ExtensionName = "GOOG_video";

        public List<GOOG_VideoSource> videos;

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            if (videos != null && videos.Count > 0)
            {
                JArray videoArr = new JArray();

                foreach (var video in videos)
                {
                    videoArr.Add(video.Serialize());
                }

                jo.Add(new JProperty(nameof(videos), videoArr));
            }

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new GOOG_Video()
            {
                videos = videos
            };
        }
    }
}
