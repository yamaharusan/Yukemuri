using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Yukemuri{
	namespace Fishing{
		public class Rod : MonoBehaviour {
			public static Rod main;

			/**@private members**/

			private bool m_releaseTriger = false;

			private float m_threadLength = 0f;

			[SerializeField]
			private int m_fishingLineNum = 10;
			[SerializeField]
			private LineRenderer m_fishingLine;
			[SerializeField]
			private LineRenderer m_fishingRodLine;

			[SerializeField]
			private Device.Controller m_controllerClass;
			private SteamVR_TrackedObject m_deviceController;

			[SerializeField]
			private Transform reelModel;

			private LineManager m_lineManager;

			private Vector3 m_currentForce;

			public LineManager GetLineManager{
				get{ return m_lineManager; }
			}

			public float ThreadLength{
				get{ return m_threadLength; }
			}

			public Vector3 CurrentForce{
				get{ return m_currentForce; }
			}

			private RodManager m_rodManagement;


			/**@public members**/

			[System.NonSerialized]
			public Rigidbody RodTip;

			public Transform Float;

			public Transform RodModelTip;

			public Text text;
			/**@private methods**/

			private void Start() {
				main = this;

				if(!gameObject.GetComponent<Rigidbody>()){
					Rigidbody rb = gameObject.AddComponent<Rigidbody>();
					rb.isKinematic = true;
					rb.useGravity =	false;
				}

				m_rodManagement = 
					new RodManager(
						this,
						gameObject,
						8,
						new Vector3(0.1f,0.1f,0.75f),
						m_fishingRodLine);

				//m_rodManagement = new RodManager(this,gameObject,RodList);

				m_lineManager = 
					new LineManager(
						this,
						RodTip,ref m_fishingLine, 
						0);

				if(m_controllerClass && m_controllerClass.ControllerTracker){
					m_deviceController = 
						m_controllerClass.ControllerTracker;
					StartCoroutine(C_Vibration());
					StartCoroutine(C_CheckThrowRod());
				}

				Float.transform.parent = m_lineManager.FishingHook.transform;
				Float.transform.localPosition = Vector3.zero;

				m_lineManager.FishingHook.transform.position = m_lineManager.Spring.transform.position;
			}

			private void Update () {
				if(m_deviceController){
					//Renew Objects
					m_lineManager.Renew();
					m_rodManagement.Renew();

					//Rod Controll
					SteamVR_Controller.Device device = SteamVR_Controller.Input((int)m_deviceController.index);

					m_currentForce = m_lineManager.Spring.currentForce;

					if(reelModel){
						reelModel.localEulerAngles = 
							new Vector3(
								0f,
								0f,
								reelModel.localEulerAngles.z+m_controllerClass.ReelAngularVelocity
							);
					}

					//釣り竿の投げに関する処理
					if(m_releaseTriger){
						//投げた直後
						if(m_threadLength == float.PositiveInfinity){
							//着水したら
							if(m_lineManager.FishingHook.transform.position.y <= 0f || m_lineManager.FishingHookInstance.IsCollision){
								m_lineManager.Spring.spring = 100000000f;

								m_threadLength = (
										m_lineManager.Spring.transform.position -
										m_lineManager.FishingHook.transform.position).magnitude;

								m_lineManager.Spring.maxDistance = m_threadLength;

								if(m_lineManager.FishingHookInstance){
									m_lineManager.FishingHookInstance.IsFishing = true;
								}
								
								GameManagement.main.startFIshing();

								Debug.Log(m_threadLength + " meter");
							}
						//ルアー着水後
						}else{

							float value = m_controllerClass.ReelAngularVelocity;

							float reelSpd = 0.0025f;

							if(m_threadLength <= 0f){
								GameManagement.main.standby();
								m_lineManager.FishingHookInstance.IsFishing = false;
							}

							if(GameManagement.main.gamePhase == GameManagement.GamePhase.ExpireYarn){
								reelSpd = 0.02f;
							}

							if(value > 0.1f){
								m_threadLength -= value * reelSpd;
							}

							GUITextLog.main.println( m_threadLength + "m" );

							GUITextLog.main.println( m_lineManager.Spring.currentForce.magnitude + "" );
							//gui_debug_3dLine.main.draw( m_lineManager.FishingHook.position, m_lineManager.FishingHook.position + m_lineManager.Spring.currentForce );

							if(m_threadLength <= 0f){

								//ゲームクリア処理
								if(m_lineManager.FishingHookInstance.GetFish){

									Vehicle.Boat.main.hangFish(m_lineManager.FishingHookInstance.GetFish);

									m_lineManager.FishingHookInstance.GetFish.DeleteFish();

									GameManagement.main.getFish();
								}

								returnLure();
							}
							
							if(m_threadLength > 8f && Random.Range(0f,1f) <= 0.05f && m_lineManager.FishingHookInstance.GetFish){
								Ray ray = new Ray(RodTip.position, m_lineManager.FishingHook.position - RodTip.position);
								RaycastHit _Hit;
								if(Physics.Raycast(ray, out _Hit, 500f, (int)Mathf.Pow(2,LayerMask.NameToLayer("Water")))){

									GameObject obj = Instantiate(GameManagement.main.effect_Splash);
									obj.transform.position = ray.origin + ray.direction.normalized * _Hit.distance;
									Destroy(obj,3f);
								}	
							}

							//Debug.Log(value);

							m_lineManager.Spring.maxDistance = m_threadLength;
						}
					}else{
						m_lineManager.Spring.maxDistance = 0f;
					}

					/*
					//Debug
					if(m_lineManager.FishingHookInstance.IsFishing)
						gui_debug_3dLine.main.setColor(Color.cyan);
					else
						gui_debug_3dLine.main.setColor(Color.red);

					gui_debug_3dLine.main.draw(Float.transform.position,Float.transform.position + Vector3.up * 500f);
					*/

				}else{
					if(m_controllerClass && m_controllerClass.ControllerTracker){
						m_deviceController = 
							m_controllerClass.ControllerTracker;

						StartCoroutine(C_Vibration());
						StartCoroutine(C_CheckThrowRod());
					}
				}
			}

			//ルアーをリリースする；
			private void releaseLure(){
				GameManagement.main.throwRod();

				m_releaseTriger = true;
				m_threadLength = float.PositiveInfinity;

				if(m_lineManager.FishingHookInstance){

					float vel = m_lineManager.FishingHookInstance.GetRigidbody.velocity.magnitude;

					Vector3 v =  m_lineManager.FishingHookInstance.GetRigidbody.velocity;
					v.Normalize();

					m_lineManager.FishingHookInstance.GetRigidbody.velocity = v*Mathf.Clamp(vel*1.25f,25f,45f);
						
						//v * 1000f;
				}

				//StartCoroutine(C_ReleaseLure());

				m_lineManager.Spring.maxDistance = m_threadLength;
				m_lineManager.Spring.spring = 0f;
			}

			private IEnumerator C_ReleaseLure(){
				yield return new WaitForFixedUpdate();

				m_lineManager.Spring.spring = 0f;

			}

			//ルアーを初期位置にもどす
			private void returnLure(){
				if(GameManagement.main.gamePhase < GameManagement.GamePhase.GetFish)
					GameManagement.main.standby();

				m_releaseTriger = false;
				m_threadLength = 0f;

				m_lineManager.FishingHookInstance.transform.position = RodTip.position;
				m_lineManager.FishingHookInstance.GetRigidbody.velocity = Vector3.zero;

				if(m_lineManager.FishingHookInstance){
					m_lineManager.FishingHookInstance.IsFishing = false;
				}
			}

			//VIVEコントローラのバイブレーション
			private IEnumerator C_Vibration(){

				yield return new WaitForSeconds(0.1f);

				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)m_deviceController.index);

				while(true){
					float s = m_lineManager.FishingHook.velocity.magnitude - RodTip.velocity.magnitude;
					s = Mathf.Clamp(s*80f,100f,2000f);

					device.TriggerHapticPulse((ushort)s);

					yield return new WaitForFixedUpdate();
				}
			}

			//釣り竿の投げ判定
			private IEnumerator C_CheckThrowRod(){

				yield return new WaitForSeconds(1f);

				bool isApproaching = false;

				float old_dot = float.NaN;

				float throwAngle = 45f;

				Vector3 throwVector = 
					Quaternion.Euler(-throwAngle,0f,0f) * Vector3.forward;

				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)m_deviceController.index);

				Rigidbody rodRb = m_rodManagement.RodPartsList[1].GetComponent<Rigidbody>();

				while(true){
					if(!m_releaseTriger && GameManagement.main.gamePhase < GameManagement.GamePhase.GetFish){

						float ang = 0f;

						if(transform.up.y < 0.3f){
							ang = Mathf.Atan2(-transform.up.x,-transform.up.z) * Mathf.Rad2Deg;
						}else{
							ang = Mathf.Atan2(transform.forward.x,transform.forward.z) * Mathf.Rad2Deg;
							if(transform.position.y < transform.position.y - transform.up.y){
								ang += 180f;
							}
						}

						GUITextLog.main.println(transform.up.y+"");

						throwVector = 
							Quaternion.Euler(-throwAngle,ang,0f) * Vector3.forward;

						Vector3 vel = m_lineManager.FishingHook.velocity;
						
						//gui_debug_3dLine.main.draw(transform.position,transform.position + throwVector);

						float dot = Vector3.Dot(throwVector,vel.normalized);

						GUITextLog.main.println(""+ rodRb.angularVelocity.x);

						if(
							rodRb.angularVelocity.x > 0.1f
						){
							if(
								old_dot != float.NaN &&
								dot > 0.5f &&
								vel.magnitude > 12f
							){
								if(isApproaching){
									vel += Camera.main.transform.forward;
									m_lineManager.FishingHook.velocity += throwVector * vel.magnitude;
									m_lineManager.FishingHook.velocity /= 2f;

									m_releaseTriger = true;
									Debug.Log(vel.magnitude);
									releaseLure();
								}
							}
						}
						
						if(old_dot > dot){
							isApproaching = true;
						}else
							isApproaching = false;

						old_dot = dot;
						
						if(device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger)){
							Debug.Log(vel.magnitude + "");
							if(vel.magnitude > 8f){
								releaseLure();
							}
						}else{

						}
					}

					/*
					gui_debug_3dLine.main.setColor(Color.red);
					gui_debug_3dLine.main.draw(
						transform.position,
						transform.position + throwVector
					);

					gui_debug_3dLine.main.setColor(Color.blue);
					gui_debug_3dLine.main.draw(
						transform.position,
						transform.position + transform.forward
					);

					Debug.Log(Vector3.Dot(throwVector,transform.forward));
					*/

					yield return new WaitForFixedUpdate();
				}
			}


			/**@public members**/


			/**@private classes**/

			private class RodManager{

				public List<GameObject> RodPartsList = new List<GameObject>();

				public List<Transform> ModelList = new List<Transform>();

				public List<HingeJoint> XHingeList = new List<HingeJoint>();
				public List<HingeJoint> YHingeList = new List<HingeJoint>();

				private LineRenderer m_lineRenderer;

				private Rod m_parent; 

				private Vector3 m_size;

				private int m_count = 0;

				private bool isCreateFirstJoint = false;

				private const string rodLayer = "FishingRod";

				public RodManager(Rod parent, GameObject obj,int num, Vector3 size,  LineRenderer lineRenderer){

					m_parent = parent;
					m_size = size;

					m_count = num;

					m_lineRenderer = lineRenderer;

					obj.layer = LayerMask.NameToLayer(rodLayer);

					CreateRod(obj, 0, true);
				}

				public RodManager(Rod parent, GameObject obj, List<Transform> modelList){

					m_parent = parent;
					
					if(modelList.Count > 0){
						ModelList = modelList;
						m_size = modelList[0].transform.localScale;
						m_count = modelList.Count-1;
					}

					//CreateRod(obj, 0, true);

					for(int i=0;i<3;i++){
						RodPartsList[i].AddComponent<FixedJoint>().connectedBody = RodPartsList[i+1].GetComponent<Rigidbody>();
					}
				}

				public void Renew(){
					if(m_lineRenderer){
						List<Vector3> list = new List<Vector3>();

						//list.Add(m_parent.RodModelTip.position);
						//gui_debug_3dLine.main.draw(m_parent.RodModelTip.position,0.1f);

						for(int i=0;i<RodPartsList.Count;i++){
							list.Add(RodPartsList[i].transform.position);
							//gui_debug_3dLine.main.draw(RodPartsList[i].transform.position,0.1f);
						}

						m_lineRenderer.positionCount = list.Count;
						m_lineRenderer.SetPositions(list.ToArray());
					}
				}

				private void CreateRod(GameObject parent,int count, bool sw){
					//オブジェクト生成段階

					//XObj
					GameObject Xobj = new GameObject();

					Xobj.name = "X";
					Xobj.layer = LayerMask.NameToLayer(rodLayer);
					Xobj.transform.parent = parent.transform;
					Xobj.transform.localPosition = Vector3.forward * m_size.z / 2f;
					Xobj.transform.localEulerAngles = Vector3.zero;

					if( sw ){ //最初の呼び出し
						Xobj.transform.localPosition = Vector3.zero;
					}else{ //2つめ以降
						m_size *= 0.95f;
					}

					//YObj
					GameObject Yobj = new GameObject();

					Yobj.name = "Y";
					Yobj.layer = LayerMask.NameToLayer(rodLayer);
					Yobj.transform.parent = Xobj.transform;
					Yobj.transform.localPosition = Vector3.zero;
					Yobj.transform.localEulerAngles = Vector3.zero;

					RodPartsList.Add(Yobj);


					//AddRigidBody
					float weight = Mathf.Clamp( ((float)m_count / (Mathf.Pow(count,1.5f)+1f))*2f ,0.5f,50f);

					Rigidbody Xrb = Xobj.AddComponent<Rigidbody>();
					Xrb.mass = weight;
					Xrb.angularDrag = 0.1f;

					Rigidbody Yrb = Yobj.AddComponent<Rigidbody>();
					Yrb.mass = weight;
					Yrb.angularDrag = 5f;

					if(count < 1){
						Xrb.isKinematic = true;
						Yrb.isKinematic = true;
					}

					/*
					//AddCollider
					BoxCollider collider = Xobj.AddComponent<BoxCollider>();
					collider.size = m_size/2f;
					collider.center = Vector3.forward * m_size.z / 2f;
					*/

					//AddJoint
					if( sw ) //Call of First
						Xobj.AddComponent<FixedJoint>().connectedBody = parent.GetComponent<Rigidbody>();

					//前のYjointに今のXjointを接続
					HingeJoint oldYJoint = parent.GetComponent<HingeJoint>();
					if(oldYJoint){
						oldYJoint.connectedBody = Xrb;
					}

					HingeJoint Xjoint = Xobj.AddComponent<HingeJoint>();
					XHingeList.Add(Xjoint);
					Xjoint.connectedBody = Yrb;

					HingeJoint Yjoint = Yobj.AddComponent<HingeJoint>();
					YHingeList.Add(Yjoint);


					//SettingJoint
					JointSpring jSpring = new JointSpring();
					jSpring.spring = Mathf.Clamp( ( 100f/Mathf.Pow(count+1,1.05f) ) * 200f,2f,500000f);  
					jSpring.damper = 0f;

					JointLimits jLimit = new JointLimits();
					jLimit.max = 0f;
					jLimit.min = 0f;
					jLimit.bounciness = 1f;

					Xjoint.axis = Vector3.right;
					Xjoint.useSpring = true;
					Xjoint.spring = jSpring;
					Xjoint.useLimits = true;
					Xjoint.limits = jLimit;

					Yjoint.axis = Vector3.up;
					Yjoint.useSpring = true;
					Yjoint.spring = jSpring;
					Yjoint.useLimits = true;
					Yjoint.limits = jLimit;


					//Next
					if(count < m_count){
						CreateRod(Yobj, count + 1, false);
					}else{

						//Delete Yjoint
						DestroyImmediate(Yjoint);

						//Add
						GameObject point = new GameObject();

						point.name = "RodTip";
						point.transform.parent = Yobj.transform;
						point.transform.localPosition = Vector3.zero;

						m_parent.RodTip = point.AddComponent<Rigidbody>();
						m_parent.RodTip.mass = 2f;

						point.AddComponent<FixedJoint>().connectedBody = Yrb;
					}
				}
			}

			public class LineManager{

				private Rod m_parent; 

				private int m_num = 0;

				private LineRenderer m_lineRenderer;

				private List<Transform> m_linePosList = new List<Transform>();

				public SpringJoint Spring;

				public Rigidbody FishingHook;
				public FishingHook FishingHookInstance;

				public LineManager(Rod parent, Rigidbody rb, ref LineRenderer line, int num){
					m_parent = parent;

					m_num = num;

					m_lineRenderer = line;
					m_lineRenderer.positionCount = num;

					//CreateLine(obj);

					FishingHook = new GameObject().AddComponent<Rigidbody>();
					FishingHook.gameObject.name = "FishingHook";
					FishingHook.mass = 2f;
					FishingHook.drag = 1f;
					FishingHook.angularDrag = 0f;

					FishingHookInstance = FishingHook.gameObject.AddComponent<FishingHook>();
					FishingHookInstance.GetParentRod = m_parent;

					SpringJoint sj = rb.gameObject.AddComponent<SpringJoint>();
					Spring = sj;

					sj.anchor = Vector3.zero;
					sj.autoConfigureConnectedAnchor = false;
					sj.connectedAnchor = Vector3.zero;

					sj.spring = 1000000f;
					sj.damper = 100f;
					sj.maxDistance = 0f;

					sj.connectedBody = FishingHook;
				}

				public void Renew(){

					Vector3[] ps ={ FishingHook.transform.position,m_parent.RodTip.transform.position };

					m_lineRenderer.positionCount = ps.Length;
					m_lineRenderer.SetPositions(ps);
				}

				private GameObject CreateLine(GameObject t) {
					GameObject obj = new GameObject();

					obj.transform.parent = t.transform;
					obj.transform.localPosition = -Vector3.up * 0.2f;
					obj.transform.parent = null;

					Rigidbody rb = obj.AddComponent<Rigidbody>();
					rb.mass = 0f;
					rb.angularDrag = 0f;
					rb.useGravity = false;

					ConfigurableJoint hj = t.AddComponent<ConfigurableJoint>();
					//hj.anchor = -Vector3.up * 0.1f;
					hj.axis = Vector3.zero;
					hj.connectedBody = obj.GetComponent<Rigidbody>();
					hj.xMotion = ConfigurableJointMotion.Locked;
					hj.yMotion = ConfigurableJointMotion.Locked;
					hj.zMotion = ConfigurableJointMotion.Locked;

					m_linePosList.Add(obj.transform);

					if(m_num > 0){

						m_num--;

						CreateLine(obj);

						return null;
					}else{
						return obj;
					}
				}
			}
		}
	}
}
