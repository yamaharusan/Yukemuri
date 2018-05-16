using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishBone : MonoBehaviour {

	public Transform[] Bones;
	private List<BoneData> data = new List<BoneData>();

	// Use this for initialization
	void Start () {
		foreach(Transform t in Bones) {
			BoneData d = new BoneData();
			d.transform = t;
			d.defaultAngles = t.localEulerAngles;

			data.Add(d);
		}
	}
	
	// Update is called once per frame
	void Update () {
		int c = 0;
		foreach(BoneData d in data) {
			d.transform.localEulerAngles = d.defaultAngles + new Vector3(0f,Mathf.Sin((Time.time)*4f) * 5f * (1+c*0.15f),0f);
			c++;
		}
	}
	
	struct BoneData{
		public Transform transform;
		public Vector3 defaultAngles;
	}
}
