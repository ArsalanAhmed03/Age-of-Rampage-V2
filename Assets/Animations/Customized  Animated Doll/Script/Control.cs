using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Control : MonoBehaviour
{
	/*
	ID:(type: interger; modify by PlayerPrefs.SetInt()/PlayerPrefs.GetInt())
		faceID : used to save/load face, 
		hairID : used to save/load hair
		headaccID : used to save/load headacc
		pantsID : used to save/load pants
		shoesID : used to save/load shoes
		topID : used to save/load top
		weaponID : used to save/load weapon
		
	Public functions to customize character:
		LoadCharacter() : Load character with faceID, hairID, headaccID,
						  pantsID, shoesID, topID, weaponID
		FaceNext() : Load the next face folder & update faceID
		FacePrev() : Load the prev face folder & update faceID
		HairNext() : Load the next hair folder & update hairID
		HairPrev() : Load the prev hair folder & update hairID
		HeadAccNext() : Load the next headacc folder & update headaccID
		HeadAccPrev() : Load the prev headacc folder & update headaccID
		PantsNext() : Load the next pants folder & update pantsID
		PantsPrev() : Load the prev pants folder & update pantsID
		ShoesNext() : Load the next shoes folder & update shoesID
		ShoesPrev() : Load the prev shoes folder & update shoesID
		TopNext() : Load the next top folder & update topID
		TopPrev() : Load the prev top folder & update topID
		WeaponNext() : Load the next weapon folder & update weaponID
		WeaponPrev() : Load the prev weapon folder & update weaponID
		AnimationNext() : Load next animation & update aniID
		AnimationPrev() : Load prev animation & update aniID
		SkinNext() : Load next skin, change top and pants skin folder & update skinID
		SkinPrev() : Load prev skin, change top and pants skin folder & update skinID
		RandomCharacter() : Load a ramdom set of character
	*/
	public GameObject 
		head,
		face,
		hair_0, hair_1, hair_2,
		headacc_0, headacc_1,
		pants_0, pants_1, pants_2, pants_3, pants_4, pants_5, pants_6,
		shoes_1, shoes_2,
		top_0, top_1, top_2, top_3, top_4, top_5, top_6, 
		weapon_0, weapon_1;
	SpriteRenderer
		spritehead,
		spriteface,
		spritehair_0, spritehair_1, spritehair_2,
		spriteheadacc_0, spriteheadacc_1,
		spritepants_0, spritepants_1, spritepants_2, spritepants_3, spritepants_4, spritepants_5, spritepants_6,
		spriteshoes_1, spriteshoes_2,
		spritetop_0, spritetop_1, spritetop_2, spritetop_3, spritetop_4, spritetop_5, spritetop_6, 
		spriteweapon_0, spriteweapon_1;
	
	// Change startNumber and endNumber based on number of the folder in resource
	int startNumber = 81;
	int endNumber = 100;
	
	string[] skinColors = {"i", "ii", "iii", "iv"}; /*i: normal, ii: light, iii: dark, iv: blue*/
	int firstSkinID = 0;
	int lastSkinID = 3;
	
	public GameObject player;
	Animator anim;
	public Text aniTypeText;
	string[] aniType = { "Idle (1)", "Idle (2)", "Idle (3)",
						"Walk", "Run forward", "Run forward with weapon", "Cast Magic",
						"Attack (1)", "Attack (2)", "Attack (3)",
						"Being attacked","Run away (lose)"};
	int firstAniID = 0;
	int lastAniID = 11;
	
    // Start is called before the first frame update
    void Start()
    {
		anim = player.GetComponent<Animator>();
        initSprite();
		ResetCharacter(); // Function to Reset character
		// LoadCharacter(); // Public Function to Load character
		initAnimation();
    }
	
	void initSprite()
	{
		spritehead		= head.GetComponent<SpriteRenderer>();
		spriteface		= face.GetComponent<SpriteRenderer>();
		spritehair_0	= hair_0.GetComponent<SpriteRenderer>();
		spritehair_1	= hair_1.GetComponent<SpriteRenderer>();
		spritehair_2	= hair_2.GetComponent<SpriteRenderer>();
		spriteheadacc_0	= headacc_0.GetComponent<SpriteRenderer>();
		spriteheadacc_1	= headacc_1.GetComponent<SpriteRenderer>();
		spritepants_0	= pants_0.GetComponent<SpriteRenderer>();
		spritepants_1	= pants_1.GetComponent<SpriteRenderer>();
		spritepants_2	= pants_2.GetComponent<SpriteRenderer>();
		spritepants_3	= pants_3.GetComponent<SpriteRenderer>();
		spritepants_4	= pants_4.GetComponent<SpriteRenderer>();
		spritepants_5	= pants_5.GetComponent<SpriteRenderer>();
		spritepants_6	= pants_6.GetComponent<SpriteRenderer>();
		spriteshoes_1	= shoes_1.GetComponent<SpriteRenderer>();
		spriteshoes_2	= shoes_2.GetComponent<SpriteRenderer>();
		spritetop_0		= top_0.GetComponent<SpriteRenderer>();
		spritetop_1		= top_1.GetComponent<SpriteRenderer>();
		spritetop_2		= top_2.GetComponent<SpriteRenderer>();
		spritetop_3		= top_3.GetComponent<SpriteRenderer>();
		spritetop_4		= top_4.GetComponent<SpriteRenderer>();
		spritetop_5		= top_5.GetComponent<SpriteRenderer>();
		spritetop_6		= top_6.GetComponent<SpriteRenderer>();
		spriteweapon_0	= weapon_0.GetComponent<SpriteRenderer>();
		spriteweapon_1	= weapon_1.GetComponent<SpriteRenderer>();
	}
	
	/* ResetCharacter() : Reset all ID value to startNumber, change character following*/
	void ResetCharacter()
	{
		int i;
		i = startNumber;
		PlayerPrefs.SetInt("faceID", i);
		ChangeFace(i);
		PlayerPrefs.SetInt("hairID", i);
		ChangeHair(i);
		PlayerPrefs.SetInt("headaccID", i);
		ChangeHeadAcc(i);
		PlayerPrefs.SetInt("pantsID", i);
		ChangePants(i);
		PlayerPrefs.SetInt("shoesID", i);
		ChangeShoes(i);
		PlayerPrefs.SetInt("topID", i);
		ChangeTop(i);
		PlayerPrefs.SetInt("weaponID", i);
		ChangeWeapon(i);
		i = firstSkinID;
		PlayerPrefs.SetInt("skinID", i);
		ChangeHead();
	}
	
	/* LoadCharacter() : Load character with faceID, hairID, headaccID, pantsID, shoesID, topID, weaponID */
	public void LoadCharacter()
	{
		int i;
		i = PlayerPrefs.GetInt("faceID");
		ChangeFace(i);
		i = PlayerPrefs.GetInt("hairID");
		ChangeHair(i);
		i = PlayerPrefs.GetInt("headaccID");
		ChangeHeadAcc(i);
		i = PlayerPrefs.GetInt("pantsID");
		ChangePants(i);
		i = PlayerPrefs.GetInt("shoesID");
		ChangeShoes(i);
		i = PlayerPrefs.GetInt("topID");
		ChangeTop(i);
		i = PlayerPrefs.GetInt("weaponID");
		ChangeWeapon(i);
		ChangeHead();
	}
	
	/* initAnimation() : Reset aniID to 0, change animatio to idle_1*/
	void initAnimation()
	{
		PlayerPrefs.SetInt("aniID", firstAniID);
		anim.SetInteger("aniID", firstAniID);
		aniTypeText.text = aniType[firstAniID];
		/* 	Animation ID:
		0	: Idle_1 (firstAniID)
		1	: Idle_2
		2	: Idle_3
		3	: Walk
		4	: Run_1
		5	: Run_2
		6	: Magic
		7	: Attack_1
		8	: Attack_2
		9	: Attack_3
		10	: Beinghit
		11	: Run away (lastAniID)
		*/
	}
	
	void ChangeHead()
	{
		int skinId = PlayerPrefs.GetInt("skinID");
		spritehead.sprite = Resources.Load<Sprite>("Head/" + skinColors[skinId] + "/Head");
	}
	
	void ChangeFace(int i)
	{
		spriteface.sprite = Resources.Load<Sprite>("Face/" + i.ToString() + "/Face");
	}
	
	void ChangeHair(int i)
	{
		spritehair_0.sprite = Resources.Load<Sprite>("Hair/" + i.ToString() + "/Hair_0");
		spritehair_1.sprite = Resources.Load<Sprite>("Hair/" + i.ToString() + "/Hair_1");
		spritehair_2.sprite = Resources.Load<Sprite>("Hair/" + i.ToString() + "/Hair_2");
	}
	
	void ChangeHeadAcc(int i)
	{
		spriteheadacc_0.sprite = Resources.Load<Sprite>("Headacc/" + i.ToString() + "/Headacc_0");
		spriteheadacc_1.sprite = Resources.Load<Sprite>("Headacc/" + i.ToString() + "/Headacc_1");
	}
	
	void ChangePants(int i)
	{
		int skinId = PlayerPrefs.GetInt("skinID");
		spritepants_0.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_0");
		spritepants_1.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_1");
		spritepants_2.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_2");
		spritepants_3.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_3");
		spritepants_4.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_4");
		spritepants_5.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_5");
		spritepants_6.sprite = Resources.Load<Sprite>("Pants/" + i.ToString() + "/" + skinColors[skinId] + "/Pants_6");
	}
	
	void ChangeShoes(int i)
	{
		spriteshoes_1.sprite = Resources.Load<Sprite>("Shoes/" + i.ToString() + "/shoes_1");
		spriteshoes_2.sprite = Resources.Load<Sprite>("Shoes/" + i.ToString() + "/shoes_2");
	}
	
	void ChangeTop(int i)
	{
		int skinId = PlayerPrefs.GetInt("skinID");
		spritetop_0.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_0");
		spritetop_1.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_1");
		spritetop_2.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_2");
		spritetop_3.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_3");
		spritetop_4.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_4");
		spritetop_5.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_5");
		spritetop_6.sprite = Resources.Load<Sprite>("Top/" + i.ToString() + "/" + skinColors[skinId] + "/Top_6");
	}
	
	void ChangeWeapon(int i)
	{
		spriteweapon_0.sprite = Resources.Load<Sprite>("Weapon/" + i.ToString() + "/Weapon_0");
		spriteweapon_1.sprite = Resources.Load<Sprite>("Weapon/" + i.ToString() + "/Weapon_1");
	}
	
	
	/********************************************************/
	/*                    PUBLIC FUNCTIONS                  */
	/********************************************************/
	/* FaceNext() : Load the next face folder & update faceID */
	public void FaceNext()
	{
		int i = PlayerPrefs.GetInt("faceID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("faceID", i);
		ChangeFace(i);
	}
	
	/* FacePrev() : Load the prev face folder & update faceID */
	public void FacePrev()
	{
		int i = PlayerPrefs.GetInt("faceID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("faceID", i);
		ChangeFace(i);
	}
	
	/* HairNext() : Load the next hair folder & update hairID */
	public void HairNext()
	{
		int i = PlayerPrefs.GetInt("hairID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("hairID", i);
		ChangeHair(i);
		
	}
	
	/* HairPrev() : Load the prev hair folder & update hairID */
	public void HairPrev()
	{
		int i = PlayerPrefs.GetInt("hairID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("hairID", i);
		ChangeHair(i);
	}
	
	/* HeadAccNext() : Load the next headacc folder & update headaccID */
	public void HeadAccNext()
	{
		int i = PlayerPrefs.GetInt("headaccID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("headaccID", i);
		ChangeHeadAcc(i);
	}
	
	/* HeadAccPrev() : Load the prev headacc folder & update headaccID */
	public void HeadAccPrev()
	{
		int i = PlayerPrefs.GetInt("headaccID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("headaccID", i);
		ChangeHeadAcc(i);
	}
	
	/* PantsNext() : Load the next pants folder & update pantsID */
	public void PantsNext()
	{
		int i = PlayerPrefs.GetInt("pantsID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("pantsID", i);
		ChangePants(i);
	}
	
	/* PantsPrev() : Load the prev pants folder & update pantsID */
	public void PantsPrev()
	{
		int i = PlayerPrefs.GetInt("pantsID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("pantsID", i);
		ChangePants(i);
	}
	
	/* ShoesNext() : Load the next shoes folder & update shoesID */
	public void ShoesNext()
	{
		int i = PlayerPrefs.GetInt("shoesID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("shoesID", i);
		ChangeShoes(i);
	}
	
	/* ShoesPrev() : Load the prev shoes folder & update shoesID */
	public void ShoesPrev()
	{
		int i = PlayerPrefs.GetInt("shoesID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("shoesID", i);
		ChangeShoes(i);
	}
	
	/* TopNext() : Load the next top folder & update topID */
	public void TopNext()
	{
		int i = PlayerPrefs.GetInt("topID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("topID", i);
		ChangeTop(i);
	}
	
	/* TopPrev() : Load the prev top folder & update topID */
	public void TopPrev()
	{
		int i = PlayerPrefs.GetInt("topID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("topID", i);
		ChangeTop(i);
	}
	
	/* WeaponNext() : Load the next weapon folder & update weaponID */
	public void WeaponNext()
	{
		int i = PlayerPrefs.GetInt("weaponID");
		i++;
		if( i > endNumber)
			{
				i = startNumber;
			}
		PlayerPrefs.SetInt("weaponID", i);
		ChangeWeapon(i);
	}
	
	/* WeaponPrev() : Load the prev weapon folder & update weaponID */
	public void WeaponPrev()
	{
		int i = PlayerPrefs.GetInt("weaponID");
		i--;
		if( i < startNumber)
			{
				i = endNumber;
			}
		PlayerPrefs.SetInt("weaponID", i);
		ChangeWeapon(i);
	}
	
	/* AnimationNext() : Load next animation & update aniID */
	public void AnimationNext()
	{
		int i = PlayerPrefs.GetInt("aniID");
		i++;
		if( i > lastAniID )
		{
			i = firstAniID;
		}
		PlayerPrefs.SetInt("aniID", i);
		aniTypeText.text = aniType[i];
		anim.SetInteger("aniID", i);
	}
	
	/* AnimationPrev() : Load prev animation & update aniID */
	public void AnimationPrev()
	{
		int i = PlayerPrefs.GetInt("aniID");
		i--;
		if( i < firstAniID )
		{
			i = lastAniID;
		}
		PlayerPrefs.SetInt("aniID", i);
		aniTypeText.text = aniType[i];
		anim.SetInteger("aniID", i);
	}
	
	/* SkinNext() : Load next skin, change top and pants skin folder & update skinID */
	public void SkinNext()
	{
		int i = PlayerPrefs.GetInt("skinID");
		i++;
		if( i > lastSkinID )
		{
			i = firstSkinID;
		}
		PlayerPrefs.SetInt("skinID", i);
		i = PlayerPrefs.GetInt("pantsID");
		ChangePants(i);
		i = PlayerPrefs.GetInt("topID");
		ChangeTop(i);
		ChangeHead();
	}
	
	/* SkinPrev() : Load prev skin, change top and pants skin folder & update skinID */
	public void SkinPrev()
	{
		int i = PlayerPrefs.GetInt("skinID");
		i--;
		if( i < firstSkinID )
		{
			i = lastSkinID;
		}
		PlayerPrefs.SetInt("skinID", i);
		i = PlayerPrefs.GetInt("pantsID");
		ChangePants(i);
		i = PlayerPrefs.GetInt("topID");
		ChangeTop(i);
		ChangeHead();
	}
	
	/* RandomCharacter() : Load a ramdome set of character */
	public void RandomCharacter()
	{
		int i;
		i = Random.Range( startNumber, endNumber );
		ChangeFace(i);
		PlayerPrefs.SetInt("faceID", i);
		ChangeHair(i);
		PlayerPrefs.SetInt("hairID", i);
		ChangeHeadAcc(i);
		PlayerPrefs.SetInt("headaccID", i);
		ChangePants(i);
		PlayerPrefs.SetInt("pantsID", i);
		ChangeShoes(i);
		PlayerPrefs.SetInt("shoesID", i);
		ChangeTop(i);
		PlayerPrefs.SetInt("topID", i);
		ChangeWeapon(i);
		PlayerPrefs.SetInt("weaponID", i);
	}
}
