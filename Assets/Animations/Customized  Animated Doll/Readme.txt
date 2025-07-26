The character can be change by changing sprite of below objects:
		head,
		face,
		hair_0, hair_1, hair_2,
		headacc_0, headacc_1,
		pants_0, pants_1, pants_2, pants_3, pants_4, pants_5, pants_6,
		shoes_1, shoes_2,
		top_0, top_1, top_2, top_3, top_4, top_5, top_6, 
		weapon_0, weapon_1;
The face has only 1 sprite,
the hair has up to 3 sprite,
the pants and top have up to 6 sprite,
...

Find the sprite in the folder match the type and replace it:
For the hair: Resources\Hair\
For the face: Resources\Face\
For the headacc: Resources\Headacc\
...

In the sample code Control.cs, there are some public functions to change:
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
You can use them as reference for creating your own functions.

Thank you for purchasing our assets!
Have fun!
Blue Goblin Store.