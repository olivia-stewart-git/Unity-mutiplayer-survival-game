using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditOptionScript : MonoBehaviour
{
    public Image optionBacking;
    public Sprite optionIcon;
    public enum EditType {destroy, repair, returnToMain}
    public EditType e_Type;
    public AnimationClip selectedClip;
}
