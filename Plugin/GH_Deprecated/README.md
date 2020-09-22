### Guildlines for changing components

All commponents that are changed should have the following linen addaed 

`public override bool Obsolete => true;`  
`public override GH_Exposure Exposure => GH_Exposure.hidden;`  
Class has to have `_Obsolete` added to to its name


and be moved to this folder, further their name should be appened by the current date

`YYMMDD_ComponentName.cs`

Further new copies of components need to have their GUIDs changed.