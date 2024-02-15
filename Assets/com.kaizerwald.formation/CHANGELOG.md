1.0.0
Initial release

1.0.1
Fix:
Add Function

Added:
UnorderedFormationMatrix<T>

1.0.2

Added:
Ability to set Formation from FormationData without creating a new object
Added to FormationMatrix and UnorderedFormationMatrix setter to CurrentFormation and TargetFormation from FormationData

1.0.3

Fixed:
- Inconsistency between Formation and FormationData Constructors.
- DefaultWidth on constructors dont allow 0 except if the number of elements of the formation is 0.

1.1.0

Added:
 - FormationMatrixBehaviour, Unity's component version of formationMatrix

Fixed:
- rewrite FormationMatrices for more clarity

1.1.1

Fixed:
- FormationMatrixBehaviour is now an abstract Monobehaviour as Generic Monobehaviour are not allowed by the engine!

1.1.2

Added:
- float3:LeaderTargetPosition is now part of FormationMatrices

1.1.3
Fixe Before Breaking Changes
Fixed:
- UnorderedFormation DestroyImmediate has now correct behaviour

1.2.0

Added:
- OrderedFormationBehaviour, UnorderedFormationBehaviour, FormationElementbehaviour

Breaking Change:
- IFormationElement bool "IsDead" replace by "IsInactive", more generic and accurate to describe the state of the element

1.2.1

Fixes:
- Remove functions logic
- Consistent typos

1.2.2

Fixes:
- UnorderedFormationBehaviour Remove function
- Formation MinRow/MaxRow now take in account NumUnitsAlive!

Added:
- Formation: BaseMinRow, BaseMaxRow to differentiate from MinRow, MaxRow

1.2.3

Added:
- FormationBehaviours have Virtual function wrappers so derived class can add behaviour before and after Event without having to subscribe to their own events