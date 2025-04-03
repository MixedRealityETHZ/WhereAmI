using UnityEngine;



//Set a different behavior for the transparent mesh overlay in editor and in game (since the game-version causes the editor to glitch)
public class TransparentMeshOverlay : MonoBehaviour
{
    void Start()
    {
        // In game -> mesh is fully transparent
        GetComponent<MeshRenderer>().material.SetFloat("_Alpha", 0f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // In editor -> mesh is not transparent at all
        GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Alpha", 1f);
    }
#endif
}
