using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
public enum Difficulty{
	Easy = 1,
	Medium = 2,
	Hard = 3,
}

public enum MonsterType{
	Humanoid = 1,
	Monster = 2,
	Npc = 3,
}



[System.Serializable]
public class ExcelExampleData
{
  [SerializeField]
  uint id;
  public uint Id { get {return id; } set { this.id = value;} }
  
  [SerializeField]
  string name;
  public string Name { get {return name; } set { this.name = value;} }
  
  [SerializeField]
  long strength;
  public long Strength { get {return strength; } set { this.strength = value;} }
  
  [SerializeField]
  Difficulty difficulty;
  public Difficulty DIFFICULTY { get {return difficulty; } set { this.difficulty = value;} }
  
  [SerializeField]
  uint properties;
  public uint Properties { get {return properties; } set { this.properties = value;} }
  
  [SerializeField]
  MonsterType monstertype;
  public MonsterType MONSTERTYPE { get {return monstertype; } set { this.monstertype = value;} }
  
}