using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
　あ

**/

namespace Yukemuri{
	namespace Fishing{
		namespace Fish{
			public class Base : MonoBehaviour {

				private FishingHook m_parentHook;

				public FishingHook GetHook{
					get{ return m_parentHook; }
				}

				private FishStatus m_fishStatus;

				public FishStatus GetFishStatus{
					get{ return m_fishStatus; }
				}

				private Rigidbody m_rigidBody;
				
				public Rigidbody GetRigidbody{
					get{ return m_rigidBody; }
				}

				private float m_tension = 0f;

				public float GetTension{
					get{ return m_tension; }
				}

				private Rod m_parentRod;

				private FixedJoint m_hookFixedJoint;

				private Rod m_rod;

				private float m_height = 5f;

				private Vector3 moveDirecton = Vector3.zero;

				public Bone Bone;

				public Transform Mouth;

				[System.NonSerialized]
				public bool IsCollision = false;

				private void Awake() {

				}

				private void Start () {

					Bone.parent = this;

					m_fishStatus = GetComponent<FishStatus>();

					Bone.model.transform.localScale = 
						Bone.model.transform.localScale * 
						(m_fishStatus.getSize()/((m_fishStatus.maxSize + m_fishStatus.minSize)/2f));

					m_rigidBody = GetComponent<Rigidbody>();
					m_rigidBody.angularDrag = 10f;

					if(m_parentHook)
						m_parentRod = m_parentHook.GetParentRod;
					else
						Destroy(gameObject);

					m_rod = m_parentHook.GetParentRod;

					StartCoroutine(C_AI());
				}
				
				private void FixedUpdate () {

					//Bone.MotionSpeed = 3f;

					if(NormalMeter.main){
						NormalMeter.main.SetValues(m_tension,m_parentRod.ThreadLength);
					}

					if(m_parentHook){
						if(m_rigidBody){
							if(m_parentHook.IsFishing && m_parentRod.ThreadLength > 3f){

								int count = 0;
								foreach(Transform t in Bone.BodyBoneList){
									if( t.position.y < 0 ){
										count++;
									}
								}

								//100 8000
								//5 400

								float m_height = Mathf.Clamp((m_parentRod.ThreadLength/50f)*5f,0.2f,5f);

								float velocity = m_rigidBody.velocity.magnitude;

								float accel = (float)count / Bone.BodyBoneList.Count;
								if(m_parentRod.ThreadLength < 3f){
									accel = 0;
								}

								float size = m_fishStatus.getSize();

								float ax = transform.localEulerAngles.x;
								if(ax > 180){
									ax = -360f + ax;
								}

								float yHeight = Mathf.Clamp(-m_height - transform.position.y,-m_height,m_height)/m_height;

								float a = 1f;
								float b = 1f;

								Vector3 to = ( transform.position - m_parentHook.GetParentRod.transform.position ).normalized;

								if(m_fishStatus.getSize() > 100){
									a = 3f;
									b = 0.9f;
									to += moveDirecton * ((1f-m_tension)*4f);
								}else{
									to += moveDirecton * ((1f-m_tension)*1f);
								}

								Vector3 local = transform.InverseTransformPoint ( transform.position + to );

								float rotx = -Mathf.Clamp( (yHeight*20)-ax, -20, 20 ) * Mathf.Deg2Rad;
								float roty = Mathf.Atan2( local.x, local.z );

								//
								float force = Mathf.Clamp(1f-(rotx / (30 * Mathf.Deg2Rad)),0f,1f);

								if(m_parentRod.ThreadLength > 8f){
									if(accel > 0.3f){
										m_rigidBody.constraints = 
											(RigidbodyConstraints)
											((int)RigidbodyConstraints.FreezeRotationX + 
											(int)RigidbodyConstraints.FreezeRotationZ);
										m_rigidBody.useGravity = false;
									}else{
										m_rigidBody.constraints = RigidbodyConstraints.FreezeRotationZ;
										m_rigidBody.useGravity = true;
									}
								}else{

									accel = 0f;

									m_rigidBody.constraints = RigidbodyConstraints.None;

									if(transform.position.y > 0){
										m_rigidBody.useGravity = true;
									}else{
										m_rigidBody.useGravity = false;
									}
								}

								m_rigidBody.mass = Mathf.Clamp((size * (size/5) * (size/15))/1000,30,180);
								m_rigidBody.drag = Mathf.Clamp(m_rigidBody.mass * (accel+1f) / 10f,10,100);
								m_rigidBody.angularDrag = m_rigidBody.drag;

								if(m_rigidBody.useGravity){
									m_rigidBody.drag = 5f;
								}

								float avrCforce = m_rigidBody.mass * (6f*(a*b));
								float f = m_parentRod.CurrentForce.magnitude / avrCforce;
								//f = 1+(1-f);

								if(f >= 1){
									f *= (1+(int)m_fishStatus.rank*0.2f);
									f = (f+7)/8;
								}else{
									f *= 0.4f;
								}

								/*
								m_rigidBody.AddForce(
									transform.forward * m_rigidBody.mass * accel * force * (1f+(m_tension*2f))
									,ForceMode.Impulse 
								);
								*/
								
								m_rigidBody.AddForceAtPosition(
									transform.forward * m_rigidBody.mass * accel * force * (0.05f+(1f-m_tension)*1.95f) * 0.4f * a
									,Mouth.transform.position
									,ForceMode.Impulse 
								);
							
								/*
								if(m_fishStatus.getSize() > 50){
									m_rigidBody.AddForce(
										transform.forward * m_rigidBody.mass * accel * force * (1f+(m_tension*2f))
										,ForceMode.Impulse 
									);
								}else{
									m_rigidBody.AddForce(
										-Vector3.up * yHeight * 5f * m_tension					
										,ForceMode.Impulse 
									);
								}
								*/
								
								m_rigidBody.AddTorque( 
									new Vector3(rotx, roty, 0f ) * m_rigidBody.mass * accel * 150f
									,ForceMode.Impulse 
								);

								

								//gui_debug_3dLine.main.draw(transform.position,transform.position + transform.forward);

								float g = rotx*Mathf.Rad2Deg;
								
								GUITextLog.main.println(rotx*Mathf.Rad2Deg+"");

								if(accel == 0f)
									g = transform.localEulerAngles.x;
								
								transform.rotation = Quaternion.Euler( g, transform.localEulerAngles.y, 0f );

								m_tension = Mathf.Clamp(m_tension+(Mathf.Clamp(f+0.05f,0.2f,1.1f)-1) * Time.deltaTime * accel,0f,1f);

								if(accel == 0){
									m_tension -= 0.3f * Time.deltaTime;
								}

							}else{
								m_tension = 0f;
								m_rigidBody.drag = 1f;
								m_rigidBody.angularDrag = 2f;
								m_rigidBody.useGravity = true;
								m_parentHook.IsHit = true;
							}
						}
					}
				}

				private IEnumerator C_AI(){
					yield return null;

					Transform t = Camera.main.transform;

					float d = 1f;
					if(Random.Range(0f,1f) > 0.5f)
						d = -1;

					float y = Random.Range(10f,30f);

					moveDirecton = Vector3.right * -d * 3f;

					int count = 0;

					while(true){
						Vector3 local = t.InverseTransformPoint ( transform.position );
						float roty = Mathf.Atan2( local.x, local.z ) * Mathf.Rad2Deg;

						Debug.Log(y*d + "   " + roty);
						
						bool b = false;
						if(d > 0){
							if(y*d < roty)
								b = true;
						} else{
							if(y*d > roty){
								b = true;
							}
						}

						if(m_tension > 0.8f){
							count++;
						}

						if(count >= 6){
							if(Random.Range(0f,1f) > 0.5f){
								count = 0;
								b = true;
							}
						}

						if(b){
							if(d > 0)
								d = -1;
							else
								d = 1;

							y = Random.Range(10f,30f);

							moveDirecton = Vector3.right * -d * 3f;

							
						}

						if(m_tension == 1f){
							GameManagement.main.expireYarn();
							DeleteFish();
							break;
						}

						if(!m_parentHook.IsFishing)
							break;

						yield return new WaitForSeconds(0.5f);
					}
				}

				public void JoinHook(FishingHook hook){
					StartCoroutine(C_JoinHook(hook));
				}

				private IEnumerator C_JoinHook(FishingHook hook){
					hook.GetFish = this;

					m_parentHook = hook;

					yield return null;
					if(hook.GetRigidbody){
						Vector3 p = transform.position;
						Vector3 hp = hook.transform.position;
						Vector3 mp = transform.position - Mouth.position;

						//Rigidbody rb = Mouth.gameObject.GetComponent<Rigidbody>();
						//if(!rb)
							//rb = Mouth.gameObject.AddComponent<Rigidbody>();
						//rb.isKinematic = true;

						transform.position = hp + mp;

						m_hookFixedJoint = gameObject.AddComponent<FixedJoint>();
						m_hookFixedJoint.connectedBody = hook.GetRigidbody;

					}
				}

				public void DeleteFish(){
					m_parentHook.GetFish = null;

					Destroy(gameObject);

					FixedJoint joint = GetComponent<FixedJoint>();
					if(joint){
						Destroy(joint);
					}
				}

				private void OnCollisionEnter(Collision collision) {
					IsCollision = true;
					StartCoroutine(UnableCollision());
				}

				private IEnumerator UnableCollision(){
					yield return null;
					IsCollision = false;
				}
			}
		}
	}
}
