using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yukemuri{
	namespace Fishing{
		public class Rod : MonoBehaviour {
			
			[SerializeField]
			private LineRenderer m_fishingLine;

			[SerializeField]
			private LineRenderer m_fishingRodLine;

			private LineManager m_lineManager;

			private RodManager m_rodManagement;

			[SerializeField]
			private SteamVR_TrackedObject rightController;

			[SerializeField]
			private int m_fishingLineNum = 10;


			[System.NonSerialized]
			public Rigidbody RodTip;

			public Transform Float;

			private bool sw = false;

			//糸のいろおれんじ

			private void Awake() {
				if(!gameObject.GetComponent<Rigidbody>()){
					Rigidbody rb = gameObject.AddComponent<Rigidbody>();

					rb.isKinematic = true;
					rb.useGravity = false;
				}

				m_rodManagement = new RodManager(this,gameObject,9,new Vector3(0.03f,0.03f,0.35f),m_fishingRodLine);

				m_lineManager = new LineManager(this,RodTip,ref m_fishingLine, 0);

				Float.transform.parent = m_lineManager.FishingHook.transform;
				Float.transform.localPosition = Vector3.zero;
			}

			// Use this for initialization
			private void Start () {
				StartCoroutine(C_Vibration());
			}
			
			// Update is called once per frame
			private void Update () {
				m_lineManager.Renew();
				m_rodManagement.Renew();

				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)rightController.index);

				//投げる時
				if (!device.GetPress(SteamVR_Controller.ButtonMask.Trigger)){//トリガーをリリースしているとき、糸をフリーにする

					if(!sw){

						if(m_lineManager.FishingHook.transform.position.y <= 0.3f){

							m_lineManager.Spring.spring = 5000f;
							m_lineManager.Spring.damper = 10f;

							m_lineManager.FishingHook.drag = 15f;
							m_lineManager.FishingHook.mass = 20f;

							m_lineManager.Spring.maxDistance = (m_lineManager.FishingHook.transform.position - RodTip.transform.position).magnitude * 0.85f;

							sw = true;

						}else{
							m_lineManager.Spring.spring = 0f;
							m_lineManager.Spring.damper = 0f;

							m_lineManager.FishingHook.drag = 0.2f;
							m_lineManager.FishingHook.mass = 0.1f;

							m_lineManager.Spring.maxDistance = 2000f;
						}
					}
				}else{//トリガーを押しているとき巻き上げる;
					//if(sw){
						m_lineManager.Spring.maxDistance =
							Mathf.Clamp(m_lineManager.Spring.maxDistance -0.06f,0.001f,2000f);//勝手に糸を巻き上げる;

						if(m_lineManager.Spring.maxDistance <= 0.1f){
							m_lineManager.Spring.maxDistance = 0.1f;
							m_lineManager.FishingHook.mass = 0.1f;
							sw = false;
						}

						m_lineManager.Spring.spring = 5000f;
						m_lineManager.Spring.damper = 10f;
					//}
					//m_lineManager.FishingHook.isKinematic = false;
				}
			}

			IEnumerator C_Vibration(){
				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)rightController.index);

				while(true){

					float s = m_lineManager.FishingHook.velocity.magnitude - RodTip.velocity.magnitude;
					s = Mathf.Clamp(s*80f,100f,2000f);

					device.TriggerHapticPulse((ushort)s);

					yield return null;
				}
			}

			private class RodManager{

				public List<GameObject> RodPartsList = new List<GameObject>();

				private LineRenderer m_lineRenderer;

				private Rod m_parent; 

				private Vector3 m_size;

				private int m_count = 0;

				public RodManager(Rod parent, GameObject obj,int num, Vector3 size,  LineRenderer lineRenderer){

					m_parent = parent;
					m_size = size;

					m_count = num;

					m_lineRenderer = lineRenderer;

					CreateRod(obj, 0, true);
				}

				public void Renew(){

					List<Vector3> list = new List<Vector3>();

					foreach(GameObject obj in RodPartsList){
						list.Add(obj.transform.position);
					}

					m_lineRenderer.positionCount = list.Count;
					m_lineRenderer.SetPositions(list.ToArray());
				}

				private GameObject CreateRod(GameObject parent,int count, bool sw){

					float weight = Mathf.Clamp(15f / ((RodPartsList.Count * RodPartsList.Count * 1f)+1),0.1f,10000f);

					JointSpring jSpring = new JointSpring();
					jSpring.spring =Mathf.Clamp(10000000f / ((RodPartsList.Count * RodPartsList.Count * 0.01f)+1),500000f,1000000f);  
					jSpring.damper = 1000f;

					GameObject jointX = new GameObject();
					jointX.name = "JointX";

					Rigidbody rbX = jointX.AddComponent<Rigidbody>();
					rbX.mass = weight;

					HingeJoint hjX = jointX.AddComponent<HingeJoint>();
					hjX.spring = jSpring;
					hjX.axis = Vector3.right;

					hjX.useLimits = true;
					hjX.useSpring = true;

					hjX.autoConfigureConnectedAnchor = false;

					if(sw){
						parent.GetComponent<Rigidbody>().isKinematic = true;

						jointX.transform.position = parent.transform.position;

						FixedJoint fj = parent.AddComponent<FixedJoint>();
						fj.connectedBody = rbX;

						hjX.connectedAnchor = Vector3.zero;

						hjX.anchor = Vector3.zero;
					}else{
						hjX.anchor = Vector3.forward * (m_size.z * (1f / 0.8f));
					}

					HingeJoint ParentHj = parent.gameObject.GetComponent<HingeJoint>();
					if(ParentHj){
						ParentHj.connectedBody = rbX;
					}



					GameObject jointY = new GameObject();
					jointY.name = "JointY";

					RodPartsList.Add(jointY);

					Rigidbody rbY = jointY.AddComponent<Rigidbody>();
					rbY.mass = weight;

					if(count < m_count){

						HingeJoint hjY = jointY.AddComponent<HingeJoint>();
						hjY.spring = jSpring;
						hjY.axis = Vector3.up;

						hjY.useLimits = true;
						hjY.useSpring = true;

						hjY.autoConfigureConnectedAnchor = false;

						hjY.anchor = Vector3.forward * (m_size.z);
						hjY.connectedAnchor = Vector3.forward * (m_size.z);

						//hjY.connectedBody = rbX;

					}

					hjX.connectedBody = rbY;

					jointX.transform.parent = parent.transform;

					jointX.transform.localEulerAngles = Vector3.zero;

					if(!sw){

						jointX.transform.localPosition = Vector3.forward * (m_size.z * (1f / 0.8f));

					}

					jointX.transform.localPosition = Vector3.forward * (m_size.z * (1f / 0.8f));

					jointX.transform.parent = null;


					jointY.transform.parent = jointX.transform;

					jointY.transform.localPosition = Vector3.zero;

					jointY.transform.parent = null;

					/*
					GameObject cube = Instantiate(m_parent.Cube);
					cube.transform.localScale = m_size;
					cube.transform.parent = jointY.transform;
					cube.transform.localPosition = -Vector3.forward * m_size.z / 2f;
					cube.transform.localEulerAngles = Vector3.zero;
					*/

					m_size *= 0.8f;

					if(count < m_count){
						CreateRod(jointY, count + 1, false);
					}else{
						GameObject point = new GameObject();
						point.name = "RodTip";

						point.transform.parent = jointY.transform;
						point.transform.localPosition = Vector3.zero;

						m_parent.RodTip = point.AddComponent<Rigidbody>();

						FixedJoint fj = point.AddComponent<FixedJoint>();
						fj.connectedBody = rbY;
					}

					return jointY;
				}
			}

			private class LineManager{

				private Rod m_parent; 

				private int m_num = 0;

				private LineRenderer m_lineRenderer;

				private List<Transform> m_linePosList = new List<Transform>();

				public SpringJoint Spring;

				public Rigidbody FishingHook;

				public LineManager(Rod parent, Rigidbody rb, ref LineRenderer line, int num){
					m_parent = parent;

					m_num = num;

					m_lineRenderer = line;
					m_lineRenderer.positionCount = num;

					//CreateLine(obj);

					FishingHook = new GameObject().AddComponent<Rigidbody>();
					FishingHook.gameObject.name = "FishingHook";
					FishingHook.drag = 1f;
					FishingHook.angularDrag = 1f;

					SpringJoint sj = rb.gameObject.AddComponent<SpringJoint>();
					Spring = sj;

					sj.anchor = Vector3.zero;
					sj.autoConfigureConnectedAnchor = false;
					sj.connectedAnchor = Vector3.zero;

					sj.spring = 500f;
					sj.damper = 50f;

					sj.maxDistance = 1f;

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
