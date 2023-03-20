using UnityEngine;
using System.Collections.Generic;
using System;

public class CharacterTest : MonoBehaviour
{
	public List<Character> characters = new List<Character>();

	// Use this for initialization
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
	}
}

[Serializable]
public class Character
{
	[SerializeField] Texture icon;

	[SerializeField] string name;

	[SerializeField] int hp;

	[SerializeField] int power;

	[SerializeField] GameObject weapon;
}