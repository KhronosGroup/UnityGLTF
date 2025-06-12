using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF
{
	internal class AvatarUtils
	{
		// A static dictionary containing the mapping from joint/bones names in the model
		// to the names Unity uses for them internally.
		// In this case they match the naming from the included Mixamo model on the left
		// and the Unity equivalent name on the right.
		// This does not need to be hard-coded.
		public static Dictionary<string, string> HumanSkeletonNames = new Dictionary<string, string>()
		{
			{"mixamorig:spine1", "Chest"},
			{"mixamorig:head", "Head" },
			{"mixamorig:hips", "Hips" },
			{"mixamorig:lefthandindex3", "Left Index Distal" },
			{"mixamorig:lefthandindex2", "Left Index Intermediate" },
			{"mixamorig:lefthandindex1", "Left Index Proximal" },
			{"mixamorig:leftHandpinky3", "Left Little Distal" },
			{"mixamorig:lefthandpinky2", "Left Little Intermediate" },
			{"mixamorig:lefthandpinky1", "Left Little Proximal" },
			{"mixamorig:lefthandmiddle3", "Left Middle Distal" },
			{"mixamorig:leftHandmiddle2", "Left Middle Intermediate" },
			{"mixamorig:lefthandmiddle1", "Left Middle Proximal" },
			{"mixamorig:lefthandring3", "Left Ring Distal" },
			{"mixamorig:lefthandring2", "Left Ring Intermediate" },
			{"mixamorig:lefthandring1", "Left Ring Proximal" },
			{"mixamorig:lefthandthumb3", "Left Thumb Distal" },
			{"mixamorig:lefthandthumb2", "Left Thumb Intermediate" },
			{"mixamorig:lefthandthumb1", "Left Thumb Proximal" },
			{"mixamorig:leftfoot", "LeftFoot" },
		    {"mixamorig:lefthand", "LeftHand" },
			{"mixamorig:leftforearm", "LeftLowerArm" },
			{"mixamorig:leftleg", "LeftLowerLeg" },
			{"mixamorig:leftshoulder", "LeftShoulder" },
			{"mixamorig:lefttoebase", "LeftToes" },
			{"mixamorig:leftarm", "LeftUpperArm" },
			{"mixamorig:leftupleg", "LeftUpperLeg" },
			{"mixamorig:neck", "Neck" },
			{"mixamorig:righthandindex3", "Right Index Distal" },
			{"mixamorig:righthandindex2", "Right Index Intermediate" },
			{"mixamorig:righthandindex1", "Right Index Proximal" },
			{"mixamorig:righthandpinky3", "Right Little Distal" },
			{"mixamorig:righthandpinky2", "Right Little Intermediate" },
			{"mixamorig:righthandpinky1", "Right Little Proximal" },
			{"mixamorig:righthandmiddle3", "Right Middle Distal" },
			{"mixamorig:righthandmiddle2", "Right Middle Intermediate" },
			{"mixamorig:righthandmiddle1", "Right Middle Proximal" },
			{"mixamorig:righthandring3", "Right Ring Distal" },
			{"mixamorig:righthandring2", "Right Ring Intermediate" },
			{"mixamorig:righthandring1", "Right Ring Proximal" },
			{"mixamorig:righthandthumb3", "Right Thumb Distal" },
			{"mixamorig:righthandthumb2", "Right Thumb Intermediate" },
			{"mixamorig:righthandthumb1", "Right Thumb Proximal" },
			{"mixamorig:rightfoot", "RightFoot" },
			{"mixamorig:righthand", "RightHand" },
			{"mixamorig:rightforearm", "RightLowerArm" },
			{"mixamorig:rightleg", "RightLowerLeg" },
			{"mixamorig:rightshoulder", "RightShoulder" },
			{"mixamorig:righttoebase", "RightToes" },
			{"mixamorig:rightarm", "RightUpperArm" },
			{"mixamorig:rightupleg", "RightUpperLeg" },
			{"mixamorig:spine", "Spine" },
			{"mixamorig:spine2", "UpperChest" },

			// Other common Avatar formats can also be added here
			// { "root", "" },
			{ "hips", "Hips" },
			{ "spine", "Spine" },
			{ "spine1", "Chest" },
			{ "chest", "Chest" },
			{ "upperchest", "UpperChest" },
			{ "neck", "Neck" },
			{ "head", "Head" },
			{ "lefteye", "LeftEye" },
			// { "eyeL_end", "" },
			{ "righteye", "RightEye" },
			// { "eyeR_end", "" },
			{ "leftshoulder", "LeftShoulder" },
			{ "leftupperarm", "LeftUpperArm" },
			{ "leftarm", "LeftUpperArm" },
			{ "leftlowerarm", "LeftLowerArm" },
			{ "leftforearm", "LeftLowerArm" },
			{ "lefthand", "LeftHand" },
			// { "leftThumbMetacarpal", "" },
			// { "leftThumbProximal", "" },
			// { "leftThumbDistal", "" },
			// { "thumbdistalL_end", "" },
			// { "leftIndexProximal", "" },
			// { "leftIndexIntermediate", "" },
			// { "leftIndexDistal", "" },
			// { "indexdistalL_end", "" },
			// { "leftMiddleProximal", "" },
			// { "leftMiddleIntermediate", "" },
			// { "leftMiddleDistal", "" },
			// { "middledistalL_end", "" },
			// { "leftRingProximal", "" },
			// { "leftRingIntermediate", "" },
			// { "leftRingDistal", "" },
			// { "ringdistalL_end", "" },
			// { "leftLittleProximal", "" },
			// { "leftLittleIntermediate", "" },
			// { "leftLittleDistal", "" },
			// { "littledistalL_end", "" },
			{ "rightshoulder", "RightShoulder" },
			{ "rightupperarm", "RightUpperArm" },
			{ "rightlowerarm", "RightLowerArm" },
			{ "righthand", "RightHand" },
			// { "rightThumbMetacarpal", "" },
			// { "rightThumbProximal", "" },
			// { "rightThumbDistal", "" },
			// { "thumbdistalR_end", "" },
			// { "rightIndexProximal", "" },
			// { "rightIndexIntermediate", "" },
			// { "rightIndexDistal", "" },
			// { "indexdistalR_end", "" },
			// { "rightMiddleProximal", "" },
			// { "rightMiddleIntermediate", "" },
			// { "rightMiddleDistal", "" },
			// { "middledistalR_end", "" },
			// { "rightRingProximal", "" },
			// { "rightRingIntermediate", "" },
			// { "rightRingDistal", "" },
			// { "ringdistalR_end", "" },
			// { "rightLittleProximal", "" },
			// { "rightLittleIntermediate", "" },
			// { "rightLittleDistal", "" },
			// { "littledistalR_end", "" },
			{ "leftupperleg", "LeftUpperLeg" },
			{ "leftupleg", "LeftUpperLeg" },
			{ "leftlowerleg", "LeftLowerLeg" },
			{ "leftleg", "LeftLowerLeg" },
			{ "leftfoot", "LeftFoot" },
			{ "lefttoes", "LeftToes" },
			{ "lefttoebase", "LeftToes" },
			// { "toesL_end", "" },
			{ "rightupperleg", "RightUpperLeg" },
			{ "rightupleg", "RightUpperLeg" },
			{ "rightlowerleg", "RightLowerLeg" },
			{ "rightleg", "RightLowerLeg" },
			{ "rightfoot", "RightFoot" },
			{ "righttoes", "RightToes" },
			{ "righttoebase", "RightToes" },
			// { "toesR_end", "" },
			
			// Meta Avatar
			{"hips_jnt", "Hips"},
			{"spinelower_jnt", "Spine"},
			{"spinemiddle_jnt", "Chest"},
			{"chest_jnt", "UpperChest"},
			{"neck_jnt", "Neck"},
			{"head_jnt", "Head"},
			{"jaw_jnt", "Jaw"},
			{"lefteye_jnt", "LeftEye"},
			{"righteye_jnt", "RightEye"},
			{"leftshoulder_jnt", "LeftShoulder"},
			{"leftarmupper_jnt", "LeftUpperArm"},
			{"leftarmlower_jnt", "LeftLowerArm"},
			{"lefthandwrist_jnt", "LeftHand"},
			{"rightshoulder_jnt", "RightShoulder"},
			{"rightarmupper_jnt", "RightUpperArm"},
			{"rightarmlower_jnt", "RightLowerArm"},
			{"righthandwrist_jnt", "RightHand"},
			{"leftlegupper_jnt", "LeftUpperLeg"},
			{"leftleglower_jnt", "LeftLowerLeg"},
			{"leftfootball_jnt", "LeftFoot"},
			{"leftfoottoe_jnt", "LeftToes"},
			{"rightlegupper_jnt", "RightUpperLeg"},
			{"rightleglower_jnt", "RightLowerLeg"},
			{"rightfootball_jnt", "RightFoot"},
			{"rightfoottoe_jnt", "RightToes"},

            // Simple bone names Unity's FBX importer can detect.
            { "lefttoe", "LeftToes" },
            { "righttoe", "RightToes" },
            { "jaw", "Jaw" },
            {"leftthumb1", "Left Thumb Proximal"},
            {"leftthumb2", "Left Thumb Intermediate"},
            {"leftthumb3", "Left Thumb Distal"},
            {"leftindex1", "Left Index Proximal" },
            {"leftindex2", "Left Index Intermediate"},
            {"leftindex3", "Left Index Distal"},
            {"leftmiddle1", "Left Middle Proximal"},
            {"leftmiddle2", "Left Middle Intermediate"},
            {"leftmiddle3", "Left Middle Distal"},
            {"leftring1", "Left Ring Proximal"},
            {"leftring2", "Left Ring Intermediate"},
            {"leftring3", "Left Ring Distal"},
            {"leftpinky1", "Left Little Proximal"},
            {"leftpinky2", "Left Little Intermediate"},
            {"leftpinky3", "Left Little Distal"},
            {"rightthumb1", "Right Thumb Proximal"},
            {"rightthumb2", "Right Thumb Intermediate"},
            {"rightthumb3", "Right Thumb Distal"},
            {"rightindex1", "Right Index Proximal"},
            {"rightindex2", "Right Index Intermediate"},
            {"rightindex3", "Right Index Distal"},
            {"rightmiddle1", "Right Middle Proximal"},
            {"rightmiddle2", "Right Middle Intermediate"},
            {"rightmiddle3", "Right Middle Distal"},
            {"rightring1", "Right Ring Proximal"},
            {"rightring2", "Right Ring Intermediate"},
            {"rightring3", "Right Ring Distal"},
            {"rightpinky1", "Right Little Proximal"},
            {"rightpinky2", "Right Little Intermediate"},
            {"rightpinky3", "Right Little Distal"},
        };

		/// <summary>
		/// Create a HumanDescription out of an avatar GameObject.
		/// The HumanDescription is what is needed to create an Avatar object
		/// using the AvatarBuilder API. This function takes care of
		/// creating the HumanDescription by going through the avatar's
		/// hierarchy, defining its T-Pose in the skeleton, and defining
		/// the transform/bone mapping in the HumanBone array.
		/// </summary>
		/// <param name="avatarRoot">Root of your avatar object</param>
		/// <returns>A HumanDescription which can be fed to the AvatarBuilder API</returns>
		public static HumanDescription CreateHumanDescription(GameObject avatarRoot)
		{
			HumanDescription description = new HumanDescription()
			{
				armStretch = 0.05f,
				feetSpacing = 0f,
				hasTranslationDoF = false,
				legStretch = 0.05f,
				lowerArmTwist = 0.5f,
				lowerLegTwist = 0.5f,
				upperArmTwist = 0.5f,
				upperLegTwist = 0.5f,
				skeleton = CreateSkeleton(avatarRoot),
				human = CreateHuman(avatarRoot),
			};
			return description;
		}

		//Create a SkeletonBone array out of an Avatar GameObject
		//This assumes that the Avatar as supplied is in a T-Pose
		//The local positions of its bones/joints are used to define this T-Pose
		private static SkeletonBone[] CreateSkeleton(GameObject avatarRoot)
		{
			List<SkeletonBone> skeleton = new List<SkeletonBone>();

			Transform[] avatarTransforms = avatarRoot.GetComponentsInChildren<Transform>();
			foreach (Transform avatarTransform in avatarTransforms)
			{
				SkeletonBone bone = new SkeletonBone()
				{
					name = avatarTransform.name,
					position = avatarTransform.localPosition,
					rotation = avatarTransform.localRotation,
					scale = avatarTransform.localScale
				};

				skeleton.Add(bone);
			}
			return skeleton.ToArray();
		}

		//Create a HumanBone array out of an Avatar GameObject
		//This is where the various bones/joints get associated with the
		//joint names that Unity understands. This is done using the
		//static dictionary defined at the top.
		private static HumanBone[] CreateHuman(GameObject avatarRoot)
		{
			List<HumanBone> human = new List<HumanBone>();

			Transform[] avatarTransforms = avatarRoot.GetComponentsInChildren<Transform>();
			foreach (Transform avatarTransform in avatarTransforms)
			{
				string humanName = avatarTransform.name.ToLowerInvariant();
				if (HumanSkeletonNames.TryGetValue(humanName, out string newHumanName)) {
					humanName = newHumanName;
				}
				else {
					// strip away trailing _1, _2, etc.
					var split = humanName.Split('_');
					var partAfterLastUnderscore = split[split.Length - 1];
					
					// if the last part is a number, remove it
					if (int.TryParse(partAfterLastUnderscore, out _))
						humanName = string.Join("_", split, 0, split.Length - 1);
					if (HumanSkeletonNames.TryGetValue(humanName, out newHumanName))
						humanName = newHumanName;
					
					// we can also try prepending "mixamorig:" to the name
					if (!HumanSkeletonNames.ContainsValue(humanName))
					{
						humanName = "mixamorig:" + humanName;
						if (HumanSkeletonNames.TryGetValue(humanName, out newHumanName))
							humanName = newHumanName;
					}
				}
				
				if (!HumanSkeletonNames.ContainsValue(humanName))
					continue;
				
				HumanBone bone = new HumanBone
				{
					boneName = avatarTransform.name,
					humanName = humanName,
					limit = new HumanLimit()
				};
				bone.limit.useDefaultValues = true;

				human.Add(bone);
			} 
			return human.ToArray();
		}
	}
}
