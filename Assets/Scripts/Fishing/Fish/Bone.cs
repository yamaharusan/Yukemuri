using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yukemuri{
	namespace Fishing{
		namespace Fish{
			public class Bone : MonoBehaviour {

				public Base parent;

				public Transform model;

				//public Transform headBone;

				public Transform bodyBone;

				public enum SwimingType{
					Small,
					Middle,
					Big
				}

				public SwimingType MySwimingType;

				public float MotionSpeed = 2f;

				public List<Transform> BodyBoneList{
					get{ return bodyBoneList; }
				}		

				public float defMotionSpeed = 0f;

				private List<Collider> colliderList = new List<Collider>();

				//private List<Transform> headBoneList = new List<Transform>();

				private List<Transform> bodyBoneList = new List<Transform>();

				//private List<BoneData> headBoneData = new List<BoneData>();

				private List<BoneData> bodyBoneData = new List<BoneData>();

				private void Awake () {

					defMotionSpeed = MotionSpeed;

					int c = 0;
					/*
					if(headBone){
						headBoneList.Add(headBone);

						BoneData b = new BoneData(headBone);
						b.defaultRotation = headBone.localEulerAngles;
						headBoneData.Add(b);

						Transform t = headBone.GetChild(0);
						while(true){
							if(!t || c > 20){
								break;
							}else{
								c++;
								headBoneList.Add(t);

								b = new BoneData(t);
								b.defaultRotation = t.localEulerAngles;

								headBoneData.Add(b);

								Transform oldT = t;
								foreach(Transform child in t){
									t = child;
									break;
								}
								if(t == oldT)
									break;
							}
						}
					}
					*/

					c = 0;

					if(bodyBone){
						bodyBoneList.Add(bodyBone);

						BoneData b = new BoneData(model);
						b.defaultRotation = model.localEulerAngles;
						bodyBoneData.Add(b);
						bodyBoneList.Add(model);

						b = new BoneData(bodyBone);
						b.defaultRotation = bodyBone.localEulerAngles;
						bodyBoneData.Add(b);
						bodyBoneList.Add(bodyBone);

						Transform t = bodyBone.GetChild(0);
						while(true){
							if(!t || c > 20){
								break;
							}else{
								c++;
								bodyBoneList.Add(t);

								b = new BoneData(t);
								b.defaultRotation = t.localEulerAngles;

								bodyBoneData.Add(b);

								Transform oldT = t;
								foreach(Transform child in t){
									t = child;
									break;
								}
								if(t == oldT)
									break;
							}
						}
					}


					if(model){
						GetColliders(model);
						StartCoroutine(SprashEffect());
					}
				}

				private void Update () {

					if(parent && parent.GetHook){

						/*
						for(int i=1;i<headBoneData.Count;i++){
							headBoneData[i].transform.localEulerAngles = 
								headBoneData[i].defaultRotation - 
									new Vector3(0f,Mathf.Sin(Time.time*MotionSpeed+(i*0.85f)) * Mathf.Clamp(Mathf.Pow(i+3,3f),1f,15f),0f);
						}
						*/
						
						bodyBoneData[0].transform.localEulerAngles = 
							bodyBoneData[0].defaultRotation - 
							new Vector3(0f,Mathf.Cos(Time.time*MotionSpeed*0.1f) * 6f,0f);
						
					}else{
						/*
						bodyBoneData[0].transform.localEulerAngles = 
							bodyBoneData[0].defaultRotation;
						*/
						//MotionSpeed = defMotionSpeed/50f;
					}

					for(int i=1;i<bodyBoneData.Count;i++){
						bodyBoneData[i].transform.localEulerAngles = 
							bodyBoneData[i].defaultRotation + 
								new Vector3(0f,Mathf.Sin(Time.time*MotionSpeed*0.1f+(i*0.15f)) * Mathf.Clamp(Mathf.Pow(i,2f),1f,20f) * (MotionSpeed/defMotionSpeed),0f);
					}
				}

				private IEnumerator SprashEffect(){
					List<Transform> AllBoneList = bodyBoneList;
					//AllBoneList.AddRange(headBoneList);

					List<Vector3> BonePos = GetPositionList(AllBoneList);
					List<Vector3> OldBonePos = BonePos;

					Debug.Log(bodyBoneList.Count);

					while (true){
						BonePos = GetPositionList(AllBoneList);
						for(int i=0;i<bodyBoneList.Count;i++){
							if (
								Random.Range(0f,1f) <= 0.002f && 
								(BonePos[i].y > 0f && OldBonePos[i].y < 0f) || (BonePos[i].y < 0f && OldBonePos[i].y > 0f) 
							){
								GameObject obj = Instantiate(GameManagement.main.effect_Splash);
								obj.transform.position  = bodyBoneList[i].transform.position;
								Destroy(obj,3f);
							}
						}
						OldBonePos = BonePos;
						yield return new WaitForSeconds(0.1f);
					}
				}

				private List<Vector3> GetPositionList(List<Transform> list){
					List<Vector3> rList = new List<Vector3>();

					foreach (Transform t in list){
						rList.Add(t.position);
					}

					return rList;
				}

				private void GetColliders(Transform t){
					foreach(Transform child in t){
						Collider c = child.GetComponent<Collider>();
						if(c){
							colliderList.Add(c);
						}

						GetColliders(child);
					}
				}

				private class BoneData{
					public Transform transform;
					public Vector3 defaultRotation;

					public BoneData(Transform t){
						transform = t;
						defaultRotation = t.localEulerAngles;
					}

					public void AddRotation(Vector3 rot){
						transform.localEulerAngles = defaultRotation + rot;
					}
				}
			}
		}
	}
}
