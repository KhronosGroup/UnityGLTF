using NUnit.Framework;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class MatrixTests
    {
        private Transform parent;
        private Transform t, t2;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            parent = new GameObject("Parent").transform;
            parent.position = new Vector3(1.5f, 2.5f, 3f);
            parent.rotation = Quaternion.Euler(new Vector3(30f, 60f, 115f));
            parent.localScale = new Vector3(0.5f, 2f, 3f);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            GameObject.Destroy(parent.gameObject);
        }

        [SetUp]
        public void SetUp()
        {
            t = new GameObject("Test").transform;
            t.SetParent(parent);
            t.localPosition = new Vector3(5f, 6f, 7f);
            t.localRotation = Quaternion.Euler(new Vector3(15f, 25f, 55f));
            t.localScale = new Vector3(1.5f, 2.5f, 4f);

            t2 = new GameObject("Test 2").transform;
            t2.SetParent(parent);
            t2.localPosition = new Vector3(15f, 16f, 17f);
            t2.localRotation = Quaternion.Euler(new Vector3(115f, 125f, 155f));
            t2.localScale = new Vector3(0.1f, 0.2f, 0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(t.gameObject);
            GameObject.Destroy(t2.gameObject);
        }

        [Test]
        public void LocalMatrix()
        {
            var matrix = t.GetWorldMatrix(worldSpace: false, rightHanded: false);

            t2.SetWorldMatrix(matrix, worldSpace: false, rightHanded: false);

            Debug.Log($"Positions: A {t.localPosition}, B {t2.localPosition}");
            Debug.Log($"Rotations: A {t.localRotation.eulerAngles}, B {t2.localRotation.eulerAngles}");
            Debug.Log($"Scales: A {t.localScale}, B {t2.localScale}");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.x, t2.localPosition.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.y, t2.localPosition.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.z, t2.localPosition.z, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.x, t2.localRotation.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.y, t2.localRotation.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.z, t2.localRotation.z, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.w, t2.localRotation.w, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.x, t2.localScale.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.y, t2.localScale.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.z, t2.localScale.z, 0.01f);
        }

        [Test]
        public void WorldMatrix()
        {
            var matrix = t.GetWorldMatrix(worldSpace: true, rightHanded: false);

            t2.SetWorldMatrix(matrix, worldSpace: true, rightHanded: false);

            Debug.Log($"Positions: A {t.position}, B {t2.position}");
            Debug.Log($"Rotations: A {t.rotation.eulerAngles}, B {t2.rotation.eulerAngles}");
            Debug.Log($"Scales: A {t.lossyScale}, B {t2.lossyScale}");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.x, t2.position.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.y, t2.position.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.z, t2.position.z, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.x, t2.rotation.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.y, t2.rotation.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.z, t2.rotation.z, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.w, t2.rotation.w, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.x, t2.lossyScale.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.y, t2.lossyScale.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.z, t2.lossyScale.z, 0.01f);
        }

        [Test]
        public void LocalMatrixRightHandedConversion()
        {
            var matrix = t.GetWorldMatrix(worldSpace: false, rightHanded: true);

            t2.SetWorldMatrix(matrix, worldSpace: false, rightHanded: true);

            Debug.Log($"Positions: A {t.localPosition}, B {t2.localPosition}");
            Debug.Log($"Rotations: A {t.localRotation.eulerAngles}, B {t2.localRotation.eulerAngles}");
            Debug.Log($"Scales: A {t.localScale}, B {t2.localScale}");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.x, t2.localPosition.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.y, t2.localPosition.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localPosition.z, t2.localPosition.z, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.x, t2.localRotation.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.y, t2.localRotation.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.z, t2.localRotation.z, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localRotation.w, t2.localRotation.w, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.x, t2.localScale.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.y, t2.localScale.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.localScale.z, t2.localScale.z, 0.01f);
        }

        [Test]
        public void WorldMatrixRightHandedConversion()
        {
            var matrix = t.GetWorldMatrix(worldSpace: true, rightHanded: true);

            t2.SetWorldMatrix(matrix, worldSpace: true, rightHanded: true);

            Debug.Log($"Positions: A {t.position}, B {t2.position}");
            Debug.Log($"Rotations: A {t.rotation.eulerAngles}, B {t2.rotation.eulerAngles}");
            Debug.Log($"Scales: A {t.lossyScale}, B {t2.lossyScale}");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.x, t2.position.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.y, t2.position.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.position.z, t2.position.z, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.x, t2.rotation.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.y, t2.rotation.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.z, t2.rotation.z, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.rotation.w, t2.rotation.w, 0.01f);

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.x, t2.lossyScale.x, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.y, t2.lossyScale.y, 0.01f);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.lossyScale.z, t2.lossyScale.z, 0.01f);
        }
    }
}