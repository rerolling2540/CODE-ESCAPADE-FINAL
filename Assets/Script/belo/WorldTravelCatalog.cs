using UnityEngine;

[CreateAssetMenu(menuName="Code Escapade/World Travel Catalog")]
public sealed class WorldTravelCatalog : ScriptableObject
{
    [System.Serializable] public class Destination
    {
        public string sceneName,title,description;
        public bool lobby;
    }
    public Destination[] destinations;
    public const string ArrivalId="world-monolith";
    public static WorldTravelCatalog Load()=>Resources.Load<WorldTravelCatalog>("Travel/WorldTravelCatalog");
}
