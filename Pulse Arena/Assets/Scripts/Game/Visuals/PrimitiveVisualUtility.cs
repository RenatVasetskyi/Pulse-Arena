using UnityEngine;

namespace Game.Visuals
{
    public static class PrimitiveVisualUtility
    {
        public static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            shader ??= Shader.Find("Sprites/Default");

            Material material = new(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            return material;
        }

        public static Transform CreatePart(string name, PrimitiveType primitiveType, Transform parent,
            Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localRotation);
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();

            if (collider != null)
                Object.Destroy(collider);

            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;

            return part.transform;
        }
    }
}
