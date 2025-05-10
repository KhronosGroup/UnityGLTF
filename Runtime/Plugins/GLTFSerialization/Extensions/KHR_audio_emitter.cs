using System;
using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
    public enum PositionalAudioDistanceModel
    {
        linear,
        inverse,
        exponential,
    }

    [Serializable]
    public class AudioEmitterId : GLTFId<KHR_AudioEmitter>
    {
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
                if (Root.Extensions.TryGetValue(KHR_audio_emitter.ExtensionName, out IExtension iextension))
                {
                    KHR_audio_emitter extension = iextension as KHR_audio_emitter;
                    return extension.emitters[Id];
                }
                else
                {
                    throw new Exception("KHR_audio not found on root object");
                }
            }
        }
        
        public static AudioEmitterId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new AudioEmitterId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    [Serializable]
    public class AudioSourceId : GLTFId<KHR_AudioSource>
    {
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
                if (Root.Extensions.TryGetValue(KHR_audio_emitter.ExtensionName, out IExtension iextension))
                {
                    KHR_audio_emitter extension = iextension as KHR_audio_emitter;
                    return extension.sources[Id];
                }
                else
                {
                    throw new Exception("KHR_audio not found on root object");
                }
            }
        }
        
        public static AudioSourceId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new AudioSourceId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    [Serializable]
    public class AudioDataId : GLTFId<KHR_AudioData>
    {
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
                if (Root.Extensions.TryGetValue(KHR_audio_emitter.ExtensionName, out IExtension iextension))
                {
                    KHR_audio_emitter extension = iextension as KHR_audio_emitter;
                    return extension.audio[Id];
                }
                else
                {
                    throw new Exception("KHR_audio not found on root object");
                }
            }
        }
        
        public static AudioDataId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new AudioDataId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    [Serializable]
    public class KHR_SceneAudioEmittersRef : IExtension
    {
        public static string ExtensionName => KHR_audio_emitter.ExtensionName;
        public List<AudioEmitterId> emitters = new List<AudioEmitterId>();

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(KHR_audio_emitter.ExtensionName, jo);

            JArray arr = new JArray();

            foreach (var emitter in emitters)
            {
                arr.Add(emitter.Id);
            }

            jo.Add(new JProperty(nameof(emitters), arr));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_SceneAudioEmittersRef() { emitters = emitters };
        }
        
        public static KHR_SceneAudioEmittersRef Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            var extension = new KHR_SceneAudioEmittersRef();

            var idsToken = extensionToken.Value[nameof(KHR_SceneAudioEmittersRef.emitters)];
            if (idsToken != null)
            {
                var ids = idsToken as JArray;
                
               // var ids = idsToken.CreateReader().ReadInt32List();
                foreach (var id in ids)
                    extension.emitters.Add(new AudioEmitterId { Id = id.DeserializeAsInt(), Root = root });
            }
            
            return extension;
        }
    }

    [Serializable]
    public class KHR_NodeAudioEmitterRef : IExtension
    {
        public static string ExtensionName => KHR_audio_emitter.ExtensionName;
        public AudioEmitterId emitter;

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(KHR_audio_emitter.ExtensionName, jo);
            jo.Add(new JProperty(nameof(emitter), emitter.Id));
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_NodeAudioEmitterRef() { emitter = emitter };
        }
        
        public static KHR_NodeAudioEmitterRef Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            var extension = new KHR_NodeAudioEmitterRef();

            var id = extensionToken.Value[nameof(KHR_NodeAudioEmitterRef.emitter)]?.ToObject<int>();
            if (id != null)
            {
                extension.emitter = new AudioEmitterId { Id = id.Value, Root = root };
            }
            
            return extension;
        }
    }

    public class PositionalEmitterData
    {
        public string shapeType;
        public float? coneInnerAngle;
        public float? coneOuterAngle;
        public float? coneOuterGain;
        public PositionalAudioDistanceModel? distanceModel;
        
        public float? maxDistance
            ;
        public float? refDistance;
        public float? rolloffFactor;
        
        public JObject Serialize()
        {
            var positional = new JObject();

            //if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
            //  positional.Add(new JProperty(nameof(coneInnerAngle), coneInnerAngle));
            //}

            //if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
            //  positional.Add(new JProperty(nameof(coneOuterAngle), coneOuterAngle));
            //}

            //if (coneOuterGain != 0.0f) {
            //  positional.Add(new JProperty(nameof(coneOuterGain), coneOuterGain));
            //}


            if (distanceModel != PositionalAudioDistanceModel.inverse)
            {
                positional.Add(new JProperty(nameof(distanceModel), distanceModel.ToString()));
            }
            
            if (maxDistance != 10000.0f)
            {
                positional.Add(new JProperty(nameof(maxDistance), maxDistance));
            }

            if (refDistance != 1.0f)
            {
                positional.Add(new JProperty(nameof(refDistance), refDistance));
            }

            //if (rolloffFactor != 1.0f) {
            //  positional.Add(new JProperty(nameof(rolloffFactor), rolloffFactor));
            //}

     
            return positional;
        }

        public static PositionalEmitterData Deserialize(GLTFRoot root, JsonReader reader)
        {
            var positional = new PositionalEmitterData();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("PositionalEmitterData must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case nameof(shapeType):
                        positional.shapeType = reader.ReadAsString();
                        break;
                    case nameof(coneInnerAngle):
                        positional.coneInnerAngle = (float)reader.ReadAsDouble();
                        break;
                    case nameof(coneOuterAngle):
                        positional.coneOuterAngle = (float)reader.ReadAsDouble();
                        break;
                    case nameof(coneOuterGain):
                        positional.coneOuterGain = (float)reader.ReadAsDouble();
                        break;
                    case nameof(distanceModel):
                        positional.distanceModel = (PositionalAudioDistanceModel)Enum.Parse(typeof(PositionalAudioDistanceModel), reader.ReadAsString());
                        break;
                    case nameof(maxDistance):
                        positional.maxDistance = (float)reader.ReadAsDouble();
                        break;
                    case nameof(refDistance):
                        positional.refDistance = (float)reader.ReadAsDouble();
                        break;
                    case nameof(rolloffFactor):
                        positional.rolloffFactor = (float)reader.ReadAsDouble();
                        break;
                }
            }

            return positional;
           
        }
    }

    [Serializable]
    public class KHR_AudioEmitter : GLTFChildOfRootProperty
    {
        public string name;
        public string type;
        public float gain;
        public List<AudioSourceId> sources = new List<AudioSourceId>();

        public PositionalEmitterData positional = null;
        
        public virtual JObject Serialize()
        {
            var jo = new JObject();

            if (!string.IsNullOrEmpty(name))
            {
                jo.Add(nameof(name), name);
            }

            jo.Add(nameof(type), type);

            jo.Add(nameof(gain), gain);
            
            if (positional != null)
            {
                jo.Add(new JProperty(nameof(positional), positional.Serialize()));
            }

            if (sources != null && sources.Count > 0)
            {
                JArray arr = new JArray();

                foreach (var source in sources)
                {
                    arr.Add(source.Id);
                }

                jo.Add(new JProperty(nameof(sources), arr));
            }

            return jo;
        }
        
        public static KHR_AudioEmitter Deserialize(GLTFRoot root, JsonReader reader)
        {
            var emitter = new KHR_AudioEmitter();
            
            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("AudioSource must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case nameof(KHR_AudioEmitter.name):
                        emitter.Name = reader.ReadAsString();
                        break;               
                    case nameof(KHR_AudioEmitter.gain):
                        emitter.gain = (float)reader.ReadAsDouble();
                        break;
                    case nameof(KHR_AudioEmitter.type):
                        emitter.type = reader.ReadAsString();
                        break;               
                    case nameof(KHR_AudioEmitter.sources):
                        var list = reader.ReadInt32List();
                        if (list == null)
                            break;
                        foreach (var source in list)
                            emitter.sources.Add(new AudioSourceId { Id = source, Root = root });
                        break;    
                    case nameof(positional):
                        emitter.positional = PositionalEmitterData.Deserialize(root, reader);
                        break;
                }
            }    
            
            return emitter;
        }
    }
    
    [Serializable]
    public class KHR_AudioSource : GLTFChildOfRootProperty
    {
        public bool? autoPlay;
        public float? gain;
        public bool? loop;
        public AudioDataId audio;

        public JObject Serialize()
        {
            var jo = new JObject();
            
            if (autoPlay != null) 
                jo.Add(nameof(autoPlay), autoPlay);

            if (gain != null) 
                jo.Add(nameof(gain), gain);

            if (loop != null) 
                jo.Add(nameof(loop), loop);
    
            if (audio != null)
                jo.Add(nameof(audio), audio.Id);

            if (!string.IsNullOrEmpty(Name))
                jo.Add("name", Name);

            return jo;
        }
        
        public static KHR_AudioSource Deserialize(GLTFRoot root, JsonReader reader)
        {
            var audioSource = new KHR_AudioSource();
            
            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("AudioSource must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case nameof(KHR_AudioSource.Name):
                        audioSource.Name = reader.ReadAsString();
                        break;               
                    case nameof(KHR_AudioSource.audio):
                        audioSource.audio = AudioDataId.Deserialize(root, reader);
                        break;
                    case nameof(KHR_AudioSource.autoPlay):
                        audioSource.autoPlay = reader.ReadAsBoolean();
                        break;               
                    case nameof(KHR_AudioSource.gain):
                        audioSource.gain = (float)reader.ReadAsDouble();
                        break;               
                    case nameof(KHR_AudioSource.loop):
                        audioSource.loop = reader.ReadAsBoolean();
                        break;               
                }
            }    

            return audioSource;
        }
    }

    [Serializable]
    public class KHR_AudioData : GLTFChildOfRootProperty
    {
        public string uri;
        public string mimeType;
        public BufferViewId bufferView;

        public JObject Serialize()
        {
            var jo = new JObject();

            if (uri != null)
            {
                jo.Add(nameof(uri), uri);
            }
            else
            {
                jo.Add(nameof(mimeType), mimeType);
                jo.Add(nameof(bufferView), bufferView.Id);
            }

            return jo;
        }
        
        public static KHR_AudioData Deserialize(GLTFRoot root, JsonReader reader)
        {
            var audioData = new KHR_AudioData();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Audio must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case nameof(KHR_AudioData.mimeType):
                        audioData.mimeType = reader.ReadAsString();
                        break;
                      case nameof(KHR_AudioData.uri):
                        audioData.uri = reader.ReadAsString();
                        break;               
                    case nameof(KHR_AudioData.bufferView):
                        audioData.bufferView = BufferViewId.Deserialize(root, reader);
                        break;               
                }
            }    
            return audioData;
        }
    }

    [Serializable]
    public class KHR_audio_emitter : IExtension
    {
        public const string ExtensionName = "KHR_audio_emitter";

        public List<KHR_AudioData> audio = new List<KHR_AudioData>();
        public List<KHR_AudioSource> sources = new List<KHR_AudioSource>();
        public List<KHR_AudioEmitter> emitters = new List<KHR_AudioEmitter>();

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            if (audio != null && audio.Count > 0)
            {
                JArray audioArr = new JArray();

                foreach (var audioData in audio)
                {
                    audioArr.Add(audioData.Serialize());
                }

                jo.Add(new JProperty(nameof(audio), audioArr));
            }


            if (sources != null && sources.Count > 0)
            {
                JArray sourceArr = new JArray();

                foreach (var source in sources)
                {
                    sourceArr.Add(source.Serialize());
                }

                jo.Add(new JProperty(nameof(sources), sourceArr));
            }

            if (emitters != null && emitters.Count > 0)
            {
                JArray emitterArr = new JArray();

                foreach (var emitter in emitters)
                {
                    emitterArr.Add(emitter.Serialize());
                }

                jo.Add(new JProperty(nameof(emitters), emitterArr));
            }

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_audio_emitter()
            {
                audio = audio,
                sources = sources,
                emitters = emitters,
            };
        }
    }
}