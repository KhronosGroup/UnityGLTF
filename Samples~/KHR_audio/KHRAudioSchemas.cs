
using System;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
  [Serializable]
  public class AudioEmitterId : GLTFId<KHR_AudioEmitter> {
    public AudioEmitterId()
    {
    }

    public AudioEmitterId(AudioEmitterId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioEmitter Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.emitters[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class AudioSourceId : GLTFId<KHR_AudioSource> {
    public AudioSourceId()
    {
    }

    public AudioSourceId(AudioSourceId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioSource Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.sources[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class AudioDataId : GLTFId<KHR_AudioData> {
    public AudioDataId()
    {
    }

    public AudioDataId(AudioDataId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioData Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.audio[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class KHR_SceneAudioEmittersRef : IExtension {
    public List<AudioEmitterId> emitters;

    public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);  

      JArray arr = new JArray();

      foreach (var emitter in emitters) {
        arr.Add(emitter.Id);
      }

      jo.Add(new JProperty(nameof(emitters), arr));

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_SceneAudioEmittersRef() { emitters = emitters };
    }
  }

  [Serializable]
  public class KHR_NodeAudioEmitterRef : IExtension {
    public AudioEmitterId emitter;

    public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);      
      jo.Add(new JProperty(nameof(emitter), emitter.Id));
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_NodeAudioEmitterRef() { emitter = emitter };
    }
  }

  [Serializable]
  public class KHR_AudioEmitter : GLTFChildOfRootProperty {

    public string type;
    public float gain;
    public List<AudioSourceId> sources;

    public virtual JObject Serialize() {
      var jo = new JObject();

      jo.Add(nameof(type), type);

      if (gain != 1.0f) {
        jo.Add(nameof(gain), gain);
      }

      if (sources != null && sources.Count > 0) {
        JArray arr = new JArray();

        foreach (var source in sources) {
          arr.Add(source.Id);
        }

        jo.Add(new JProperty(nameof(sources), arr));
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_PositionalAudioEmitter : KHR_AudioEmitter {

    public float coneInnerAngle;
    public float coneOuterAngle;
    public float coneOuterGain;
    public PositionalAudioDistanceModel distanceModel;
    public float maxDistance;
    public float refDistance;
    public float rolloffFactor;

    public override JObject Serialize() {
      var jo = base.Serialize();

      var positional = new JObject();

      if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
        positional.Add(new JProperty(nameof(coneInnerAngle), coneInnerAngle));
      }
      
      if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
        positional.Add(new JProperty(nameof(coneOuterAngle), coneOuterAngle));
      }

      if (coneOuterGain != 0.0f) {
        positional.Add(new JProperty(nameof(coneOuterGain), coneOuterGain));
      }
      
      if (distanceModel != PositionalAudioDistanceModel.inverse) {
        positional.Add(new JProperty(nameof(distanceModel), distanceModel.ToString()));
      }
      
      if (maxDistance != 10000.0f) {
        positional.Add(new JProperty(nameof(maxDistance), maxDistance));
      }
      
      if (refDistance != 1.0f) {
        positional.Add(new JProperty(nameof(refDistance), refDistance));
      }
      
      if (rolloffFactor != 1.0f) {
        positional.Add(new JProperty(nameof(rolloffFactor), rolloffFactor));
      }

      jo.Add("positional", positional);

      return jo;
    }
  }

  [Serializable]
  public class KHR_AudioSource : GLTFChildOfRootProperty {

    public bool autoPlay;
    public float gain;
    public bool loop;
    public AudioDataId audio;

    public JObject Serialize() {
      var jo = new JObject();

      if (autoPlay) {
        jo.Add(nameof(autoPlay), autoPlay);
      }

      if (gain != 1.0f) {
        jo.Add(nameof(gain), gain);
      }
      
      if (loop) {
        jo.Add(nameof(loop), loop);
      }
      
      if (audio != null) {
        jo.Add(nameof(audio), audio.Id);  
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_AudioData : GLTFChildOfRootProperty {

    public string uri;
    public string mimeType;
    public BufferViewId bufferView;

    public JObject Serialize() {
      var jo = new JObject();

      if (uri != null) {
        jo.Add(nameof(uri), uri);
      } else {
        jo.Add(nameof(mimeType), mimeType);
        jo.Add(nameof(bufferView), bufferView.Id);
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_audio : IExtension
  {
    public const string ExtensionName = "KHR_audio";

    public List<KHR_AudioData> audio;
    public List<KHR_AudioSource> sources;
    public List<KHR_AudioEmitter> emitters;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      if (audio != null && audio.Count > 0) {
        JArray audioArr = new JArray();

        foreach (var audioData in audio) {
          audioArr.Add(audioData.Serialize());
        }

        jo.Add(new JProperty(nameof(audio), audioArr));
      }
    

      if (sources != null && sources.Count > 0) {
        JArray sourceArr = new JArray();

        foreach (var source in sources) {
          sourceArr.Add(source.Serialize());
        }

        jo.Add(new JProperty(nameof(sources), sourceArr));
      }

      if (emitters != null && emitters.Count > 0) {
        JArray emitterArr = new JArray();

        foreach (var emitter in emitters) {
          emitterArr.Add(emitter.Serialize());
        }

        jo.Add(new JProperty(nameof(emitters), emitterArr));
      }

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_audio() {
        audio = audio,
        sources = sources,
        emitters = emitters,
      };
    }
  }
}
