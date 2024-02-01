using Unity.Mathematics;

namespace Kaizerwald.FormationModule
{
    public interface IFormationInfo
    {
        public int BaseNumUnits { get; } 
        public int2 MinMaxRow { get; } 
        public float2 UnitSize { get; } 
        public float SpaceBetweenUnit { get; } 
    }
}