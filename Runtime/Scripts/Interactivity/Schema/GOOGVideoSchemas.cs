
using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
    
  //[Serializable]
  //public class VideoEmitterId : GLTFId<GOOG_VideoData> {
  //  public VideoEmitterId()
  //  {
  //  }

  //  public VideoEmitterId(VideoEmitterId id, GLTFRoot newRoot) : base(id, newRoot)
  //  {
  //  }

  //  public override GOOG_VideoData Value
  //  {
  //    get
  //    {
  //      if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
  //      {
  //        GOOG_Video extension = iextension as GOOG_Video;
  //                  return extension.videoData[Id];
  //      }
  //      else
  //      {
  //        throw new Exception("GOOG_Video not found on root object");
  //      }
  //    }
  //  }
  //}

//  [Serializable]
  //public class AudioSourceId : GLTFId<KHR_AudioSource> {
  //  public AudioSourceId()
  //  {
  //  }

  //  public AudioSourceId(AudioSourceId id, GLTFRoot newRoot) : base(id, newRoot)
  //  {
  //  }

  //  public override KHR_AudioSource Value
  //  {
  //    get
  //    {
  //      if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
  //      {
  //        KHR_audio extension = iextension as KHR_audio;
  //        return extension.sources[Id];
  //      }
  //      else
  //      {
  //        throw new Exception("KHR_audio not found on root object");
  //      }
  //    }
  //  }
  //}

  [Serializable]
  public class VideoDataId : GLTFId<GOOG_VideoData> {
    public VideoDataId()
    {
    }

    public VideoDataId(VideoDataId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override GOOG_VideoData Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          GOOG_Video extension = iextension as GOOG_Video;
          return extension.videoData[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  //[Serializable]
  //public class KHR_SceneAudioEmittersRef : IExtension {
  //  public List<AudioEmitterId> emitters;

  //  public JProperty Serialize() {
  //    var jo = new JObject();
  //    JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);  

  //    JArray arr = new JArray();

  //    foreach (var emitter in emitters) {
  //      arr.Add(emitter.Id);
  //    }

  //    jo.Add(new JProperty(nameof(emitters), arr));

  //    return jProperty;
  //  }

  //  public IExtension Clone(GLTFRoot root)
  //  {
  //    return new KHR_SceneAudioEmittersRef() { emitters = emitters };
  //  }
  //}

  //[Serializable]
  //public class KHR_NodeAudioEmitterRef : IExtension {
  //  public AudioEmitterId emitter;

  //  public JProperty Serialize() {
  //    var jo = new JObject();
  //    JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);      
  //    jo.Add(new JProperty(nameof(emitter), emitter.Id));
  //    return jProperty;
  //  }

  //  public IExtension Clone(GLTFRoot root)
  //  {
  //    return new KHR_NodeAudioEmitterRef() { emitter = emitter };
  //  }
  //}



    [Serializable]
    public class GOOG_VideoData : GLTFChildOfRootProperty {
        public string name;
        public bool autoPlay;
        public bool loop;
        public float speed;
        public int video;

        public JObject Serialize() {
            var jo = new JObject();

            if (!string.IsNullOrEmpty(name))
                jo.Add(nameof(name), name);

            jo.Add(nameof(autoPlay), autoPlay);
            jo.Add(nameof(loop), loop);
            jo.Add(nameof(speed), speed);
            jo.Add(nameof(video), video);  
  
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
    public class GOOG_Video : IExtension
    {
        public const string ExtensionName = "GOOG_video";

        public List<GOOG_VideoSource> videoSource;
        public List<GOOG_VideoData> videoData;

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            if (videoSource != null && videoSource.Count > 0)
            {
                JArray videoArr = new JArray();

                foreach (var audioData in videoSource)
                {
                    videoArr.Add(audioData.Serialize());
                }

                jo.Add(new JProperty(nameof(videoSource), videoArr));
            }

            if (videoData != null && videoData.Count > 0)
            {
                JArray videoDataArr = new JArray();

                foreach (var data in videoData)
                {
                    videoDataArr.Add(data.Serialize());
                }

                jo.Add(new JProperty(nameof(videoData), videoDataArr));
            }

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new GOOG_Video()
            {
                videoData = videoData,
                videoSource = videoSource
            };
        }
    }
}
