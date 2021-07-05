using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Color = UnityEngine.Color;
using Kinect = Windows.Kinect;
using UnityEngine.UI;
using Joint = Windows.Kinect.Joint;

public class BodySourceView : MonoBehaviour 
{
    public Material BoneMaterial;
    public GameObject BodySourceManager;
    private Dictionary<ulong, (GameObject, bool)> _Bodies = new Dictionary<ulong, (GameObject, bool)>();
    private BodySourceManager _BodyManager;
    private bool flag = false;


    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        // { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        // { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        // { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        // { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        // { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        // { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        // { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        // { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        // { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        // { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        // { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        // { Kinect.JointType.Neck, Kinect.JointType.Head },
        
        //new
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Head },
    };
    
    private List<Kinect.JointType> _Joints = new List<Kinect.JointType>()
    {
        Kinect.JointType.AnkleLeft,
        Kinect.JointType.KneeLeft,
        Kinect.JointType.HipLeft,
        Kinect.JointType.SpineBase,
        Kinect.JointType.AnkleRight,
        Kinect.JointType.KneeRight,
        Kinect.JointType.HipRight,
        Kinect.JointType.WristLeft,
        Kinect.JointType.ElbowLeft,
        Kinect.JointType.ShoulderLeft,
        Kinect.JointType.WristRight,
        Kinect.JointType.ElbowRight,
        Kinect.JointType.ShoulderRight,
        Kinect.JointType.SpineShoulder,
        Kinect.JointType.Head,
    };

    public int skin_index = 1;
    public bool jump = false;
    public GameObject[] headObject = new GameObject[5];
    public GameObject armObject;
    public GameObject legObject;
    public GameObject chestObject;
    public GameObject menuObject;
    public GameObject[] fadona; 


    private Queue<float> _posicoesTornozelo = new Queue<float>();
    private Queue<float> _posicoesOmbros = new Queue<float>();
    private Queue<float> _distanciasEsquerda = new Queue<float>();
    private Queue<float> _distanciasDireita = new Queue<float>();

    void Update () {
        if (BodySourceManager == null)
        {
            return;
        }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            // ################ DEBUG

            if (Input.GetKeyUp(KeyCode.X))
            {
                GameObject[]  gameObj = GameObject.FindGameObjectsWithTag("MenuObject");
                //Debug.Log("APARECE MENU");
                foreach (GameObject g in gameObj)
                {
                    //Debug.Log("ABRIR MENU");
                    //Debug.Log(g.name);
                    g.GetComponent<SpriteRenderer>().enabled = !g.GetComponent<SpriteRenderer>().enabled;
                    if (g.GetComponent<SpriteRenderer>().enabled == true)
                    {
                        int selected=5;

                        if (selected != 0)
                        {
                            for (int i = 0; i < gameObj.Length; i++)
                            {
                                if (i == selected - 1)
                                {
                                    Debug.Log(gameObj[i].name);
                                }
                            }

                        }
                    }

                }
            }
            return;
        }
        
        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }
                
            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId].Item1);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
            
            if(body.IsTracked)
            {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = (CreateBodyObject(body.TrackingId), false);
                }
                
                RefreshBodyObject(body, body.TrackingId);
            }
        }

    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        fadona = GameObject.FindGameObjectsWithTag("fada");
        fadona[0].GetComponent<SpriteRenderer>().enabled = false;

        GameObject body = new GameObject("Body:" + id);
        // create head tracker
        GameObject head = Instantiate(headObject[skin_index - 1]);
        head.name = "CharacterHead";
        head.transform.parent = body.transform;
        // create chest tracker
        GameObject chest = Instantiate(chestObject);
        chest.name = "CharacterChest";
        chest.transform.parent = body.transform;
        // create left arm tracker
        GameObject leftArm = Instantiate(armObject);
        leftArm.name = "CharacterLeftArm";
        leftArm.transform.parent = body.transform;
        // create right arm tracker
        GameObject rightArm = Instantiate(armObject);
        rightArm.name = "CharacterRightArm";
        rightArm.transform.parent = body.transform;
        // create left leg tracker
        GameObject leftLeg = Instantiate(legObject);
        leftLeg.name = "CharacterLeftLeg";
        leftLeg.transform.parent = body.transform;
        // create right leg tracker
        GameObject rightLeg = Instantiate(legObject);
        rightLeg.name = "CharacterRightLeg";
        rightLeg.transform.parent = body.transform;

        return body;
    }
    
    private void RefreshBodyObject(Kinect.Body body, ulong bodyID)
    {
        GameObject[] backObj = GameObject.FindGameObjectsWithTag("Background");
        Vector3 coord = backObj[0].transform.position;

        GameObject[] maca = GameObject.FindGameObjectsWithTag("apple");
        GameObject[] luva = GameObject.FindGameObjectsWithTag("luva");

        // Ver a posicao dos objetos que vao interagir no cenario para quando fizermos swipe aparecerem e desaparecerem os objetos
        if ((coord.x > 30 && coord.x < 64) && detect_swipe(body) && maca[0].GetComponent<SpriteRenderer>().enabled == true)
        {
            fadona[0].GetComponent<SpriteRenderer>().enabled = true;
            maca[0].GetComponent<SpriteRenderer>().enabled = false;
        }

        if ((coord.x < -50) && detect_swipe(body) && luva[0].GetComponent<SpriteRenderer>().enabled == true && maca[0].GetComponent<SpriteRenderer>().enabled == false)
        {
            fadona[0].GetComponent<SpriteRenderer>().enabled = false;
            luva[0].GetComponent<SpriteRenderer>().enabled = false;
            jump = true;

        }


        // update body parts
        update_head(body, _Bodies[bodyID].Item1);
        update_left_arm(body, _Bodies[bodyID].Item1);
        update_right_arm(body, _Bodies[bodyID].Item1);
        update_chest(body, _Bodies[bodyID].Item1);
        update_left_leg(body, _Bodies[bodyID].Item1);
        update_right_leg(body, _Bodies[bodyID].Item1);
       

        //check for actions and update text
        //Text txt = GameObject.Find("Canvas/Text").GetComponent<Text>();
        var selected = detect_select(body, 5);
        //if (selected != 0) txt.text = "SELECTED: " + selected;
        //if (detect_swipe(body)) txt.text = "SWIPE";

        // ####################### Abrir o menu no salto
        if (jump)
        {
            if (!_Bodies[bodyID].Item2 && detect_jump(body))
            {
                flag = true;
                // jump = true;
                _Bodies[bodyID] = (_Bodies[bodyID].Item1, true);
                //txt.text = "JUMP";
                GameObject menu = Instantiate(menuObject);
                menu.name = "Menu";
                menu.transform.parent = _Bodies[bodyID].Item1.transform;
            }
            else
            {
                if (detect_swipe(body) && _Bodies[bodyID].Item2)
                {
                    flag = false;
                    _Bodies[bodyID] = (_Bodies[bodyID].Item1, false);
                    Destroy(_Bodies[bodyID].Item1.transform.Find("Menu").gameObject);
                }
            }
            if (_Bodies[bodyID].Item2)
            {
                Joint jt = body.Joints[Kinect.JointType.SpineMid];
                Vector3 targetPosition = GetVector3FromJoint(jt);
                targetPosition.z = 0;
                targetPosition.x = 8;
                Transform jointObject = _Bodies[bodyID].Item1.transform.Find("Menu");
                jointObject.position = targetPosition;
                selected = detect_select(body, 5);
                if (selected != 0)
                {
                    //txt.text = "SELECTED: " + selected;
                    if (selected != skin_index)
                    {
                        //if (detect_swipe(body))
                        //{
                        skin_index = selected;
                        Destroy(_Bodies[bodyID].Item1.transform.Find("CharacterHead").gameObject);
                        GameObject head = Instantiate(headObject[skin_index - 1]);
                        head.name = "CharacterHead";
                        head.transform.parent = _Bodies[bodyID].Item1.transform;
                        //}
                    }
                }
            }
        }
        var side = detect_side_walking(body);

        coord = backObj[0].transform.position; 
        if (side != 0 && !flag) {
            if (side == -1) //LEFT
            {
               
                if (coord.x < 98)
                {
                    coord.x += (float)0.5;
                    Debug.Log("L " + coord.x);
                    backObj[0].transform.position = coord;
                    //Destroy(_Bodies[bodyID].Item1.transform.Find("Background").gameObject);
                }

            }
            else if(side == 1) //RIGHT
            {
                if (coord.x > -92)
                {

                    //Debug.Log(coord);
                    coord.x -= (float)0.5;
                    Debug.Log("R " + coord.x);
                    backObj[0].transform.position = coord;
                    //Destroy(_Bodies[bodyID].Item1.transform.Find("Background").gameObject);
                }

            }

        }

        bool end = detect_close(body);
        if (end)
        {
            GameObject[] telObj = GameObject.FindGameObjectsWithTag("Tela");
            Vector3 coordy = telObj[0].transform.position;

            coordy.y -= 2;
            //Debug.Log(coordy.y);
            if (coordy.y > 13) { 
                telObj[0].transform.position = coordy; 
            }

        }
    }

    private void update_head(Kinect.Body body, GameObject bodyObject) {
        Joint jt = body.Joints[Kinect.JointType.Neck];
        Vector3 targetPosition = GetVector3FromJoint(jt);
        targetPosition.z = 0;
        targetPosition.y = targetPosition.y + (float)2.5;
        Transform jointObject = bodyObject.transform.Find("CharacterHead");
        jointObject.position = targetPosition;
    }
    
    private void update_chest(Kinect.Body body, GameObject bodyObject) {
        Joint jt = body.Joints[Kinect.JointType.SpineMid];
        Vector3 targetPosition = GetVector3FromJoint(jt);
        targetPosition.z = 0;
        targetPosition.y = targetPosition.y + 1;
        Transform jointObject = bodyObject.transform.Find("CharacterChest");
        jointObject.position = targetPosition;
    }
    
    private void update_right_arm(Kinect.Body body, GameObject bodyObject) {
        // get shoulder position
        var shoulderJoint = body.Joints[Kinect.JointType.ShoulderRight];
        Vector3 targetPosition = GetVector3FromJoint(shoulderJoint);
        targetPosition.z = 0;
        targetPosition.x = targetPosition.x + (float)0.5;
        // get arm angle
        var shoulder = shoulderJoint.Position;
        var hip = body.Joints[Kinect.JointType.HipRight].Position;
        var wrist = body.Joints[Kinect.JointType.WristRight].Position;
        var alfa = angle(shoulder, wrist, hip);
        Transform jointObject = bodyObject.transform.Find("CharacterRightArm");
        jointObject.position = targetPosition;
        jointObject.eulerAngles  = new Vector3(
            0,
            180,
            -alfa + 75
        );
    }
    
    private void update_left_arm(Kinect.Body body, GameObject bodyObject) {
        // get shoulder position
        var shoulderJoint = body.Joints[Kinect.JointType.ShoulderLeft];
        Vector3 targetPosition = GetVector3FromJoint(shoulderJoint);
        targetPosition.z = 0;
        targetPosition.x = targetPosition.x - (float)0.5;
        // get arm angle
        var shoulder = shoulderJoint.Position;
        var hip = body.Joints[Kinect.JointType.HipLeft].Position;
        var wrist = body.Joints[Kinect.JointType.WristLeft].Position;
        var alfa = angle(shoulder, wrist, hip);
        Transform jointObject = bodyObject.transform.Find("CharacterLeftArm");
        jointObject.position = targetPosition;
        
        jointObject.eulerAngles  = new Vector3(
            0,
            0,
            -alfa + 75
        );
    }
    
    private void update_right_leg(Kinect.Body body, GameObject bodyObject) {
        // get hip position
        var hipJoint = body.Joints[Kinect.JointType.HipRight];
        Vector3 targetPosition = GetVector3FromJoint(hipJoint);
        targetPosition.z = 0;
        // get leg angle
        var hip = hipJoint.Position;
        var shoulder = body.Joints[Kinect.JointType.ShoulderRight].Position;
        var ankle = body.Joints[Kinect.JointType.AnkleRight].Position;
        var alfa = angle(hip, shoulder, ankle);
        targetPosition.x -= Mathf.Sin(alfa/180*2*Mathf.PI)*(hip.Y - ankle.Y);
        Transform jointObject = bodyObject.transform.Find("CharacterRightLeg");
        jointObject.position = targetPosition;
        jointObject.eulerAngles  = new Vector3(
            0,
            180,
            alfa - 165
        );
    }
    
    private void update_left_leg(Kinect.Body body, GameObject bodyObject) {
        // get hip position
        var hipJoint = body.Joints[Kinect.JointType.HipLeft];
        Vector3 targetPosition = GetVector3FromJoint(hipJoint);
        targetPosition.z = 0;
        // get leg angle
        var hip = hipJoint.Position;
        var shoulder = body.Joints[Kinect.JointType.ShoulderLeft].Position;
        var ankle = body.Joints[Kinect.JointType.AnkleLeft].Position;
        var alfa = angle(hip, shoulder, ankle);
        targetPosition.x += Mathf.Sin(alfa/180*2*Mathf.PI)*(hip.Y - ankle.Y);
        Transform jointObject = bodyObject.transform.Find("CharacterLeftLeg");
        jointObject.position = targetPosition;
        jointObject.eulerAngles  = new Vector3(
            0,
            0,
            alfa - 165
        );
    }


    private int detect_side_walking(Kinect.Body body) {
        var left_hip = body.Joints[Kinect.JointType.HipLeft].Position.X;
        var right_hip = body.Joints[Kinect.JointType.HipRight].Position.X;
        var left_shoulder = body.Joints[Kinect.JointType.ShoulderLeft].Position.X;
        var right_shoulder = body.Joints[Kinect.JointType.ShoulderRight].Position.X;
        var diff_larguras = (right_shoulder - left_shoulder) - (right_hip - left_hip);
        var th = 0.1; // sensibilidade de deteção, quanto mais baixo, mais é preciso inclinar para detetar
        if (left_hip - left_shoulder < th * diff_larguras) return 1;
        if (right_shoulder - right_hip < th * diff_larguras) return -1;
        return 0;
    }

    private bool detect_close(Kinect.Body body) {
        var right_wrist = body.Joints[Kinect.JointType.WristRight].Position;
        var right_elbow = body.Joints[Kinect.JointType.ElbowRight].Position;
        var right_shoulder = body.Joints[Kinect.JointType.ShoulderRight].Position;
        
        var left_wrist = body.Joints[Kinect.JointType.WristLeft].Position;
        var left_elbow = body.Joints[Kinect.JointType.ElbowLeft].Position;
        var left_shoulder = body.Joints[Kinect.JointType.ShoulderLeft].Position;

        var alfa_right = Mathf.Abs(90 - angle(right_elbow, right_wrist, right_shoulder));
        var alfa_left = Mathf.Abs(90 - angle(left_elbow, left_wrist, left_shoulder));
        var th = 25; // sensibilidade de deteção, quanto mais baixo mais retos têm que estar os braços

        if (alfa_right < th && alfa_left < th) {
            _posicoesOmbros.Enqueue(right_shoulder.Y);
            _posicoesOmbros.Enqueue(left_shoulder.Y);

            if (_posicoesOmbros.Count > 50)
            {
                _posicoesOmbros.Dequeue();
                _posicoesOmbros.Dequeue();
            }
            float average = 0, std = 0;
            foreach (float f in _posicoesOmbros)
            {
                average += f;
            }
            average = average / _posicoesOmbros.Count;
            foreach (float f in _posicoesOmbros)
            {
                std += (f - average) * (f - average);
            }
            std = 10 * Mathf.Sqrt(std / _posicoesOmbros.Count);
            return left_wrist.Y > average - std && left_wrist.Y < average + std && right_wrist.Y > average - std &&
                   right_wrist.Y < average + std;
        }

        return false;
    }

    private int detect_select(Kinect.Body body, int selectibles) {
        var wrist = body.Joints[Kinect.JointType.WristRight].Position;
        var elbow = body.Joints[Kinect.JointType.ElbowRight].Position;
        var right_shoulder = body.Joints[Kinect.JointType.ShoulderRight].Position;
        var left_shoulder = body.Joints[Kinect.JointType.ShoulderLeft].Position;
        var alfa = angle(elbow, wrist, right_shoulder);
        var th = 150; //sensibilidade de deteção, quanto mais alto mais esticado tem que estar o braço para ser considerado uma seleção
        if (alfa > th) {
            right_shoulder.Y = left_shoulder.Y;
            alfa = angle(right_shoulder, left_shoulder, wrist);
            if (wrist.Y > right_shoulder.Y) {
                alfa = alfa - 90;
            } else {
                alfa = -alfa + 270;
            }
            if (alfa < 0 || alfa > 180) return 0;
            var each_selectible = 180 / selectibles;
            return Mathf.FloorToInt(alfa / each_selectible) + 1;
        }
        return 0;
    }

    private int detect_select_object(Kinect.Body body, int selectibles)
    {
        var wrist = body.Joints[Kinect.JointType.WristRight].Position;
        var elbow = body.Joints[Kinect.JointType.ElbowRight].Position;
        var right_shoulder = body.Joints[Kinect.JointType.ShoulderRight].Position;
        var left_shoulder = body.Joints[Kinect.JointType.ShoulderLeft].Position;
        var alfa = angle(elbow, wrist, right_shoulder);
        var th = 150; //sensibilidade de deteção, quanto mais alto mais esticado tem que estar o braço para ser considerado uma seleção
        if (alfa > th)
        {
            right_shoulder.Y = left_shoulder.Y;
            alfa = angle(right_shoulder, left_shoulder, wrist);
            if (wrist.Y > right_shoulder.Y)
            {
                alfa = alfa - 90;
            }
            else
            {
                alfa = -alfa + 270;
            }
            if (alfa < 0 || alfa > 180) return 0;
            var each_selectible = 180 / selectibles;
            return Mathf.FloorToInt(alfa / each_selectible) + 1;
        }
        return 0;
    }


    //calcula angulo em que P1 é vértice no plano xOy
    private float angle(Kinect.CameraSpacePoint p1, Kinect.CameraSpacePoint p2, Kinect.CameraSpacePoint p3) {
        var p12 = distance(p1, p2);
        var p13 = distance(p1, p3);
        var p23 = distance(p2, p3);
        return Mathf.Acos((p12 * p12 + p13 * p13 - p23 * p23) / (2 * p12 * p13)) * 180 / Mathf.PI;
    }
    
    //calcula distancia entre 2 pontos no plano xOy
    private float distance(Kinect.CameraSpacePoint p1, Kinect.CameraSpacePoint p2) {
        return Mathf.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
    }
    
    private bool detect_swipe(Kinect.Body body)
    {
        var mao = body.Joints[Kinect.JointType.WristRight].Position;
        var cotovelo = body.Joints[Kinect.JointType.ElbowRight].Position;
        var ombro = body.Joints[Kinect.JointType.ShoulderRight].Position;
        var th = 15; // sensibilidade de deteção, quanto mais baixo mais perto tem que estar a mão do ombro
        return angle(cotovelo, mao, ombro) < th;
    }

    private bool detect_jump(Kinect.Body body)
    {
        var ankleL = body.Joints[Kinect.JointType.AnkleLeft].Position.Y * 10;
        var ankleR = body.Joints[Kinect.JointType.AnkleRight].Position.Y * 10;
        _posicoesTornozelo.Enqueue(ankleL);
        _posicoesTornozelo.Enqueue(ankleR);

        if (_posicoesTornozelo.Count > 50)
        {
            _posicoesTornozelo.Dequeue();
            _posicoesTornozelo.Dequeue();
        }
        float average, std;
        average = 0;
        std = 0;
        foreach (float f in _posicoesTornozelo)
        {
            average += f;
        }
        average = average / _posicoesTornozelo.Count;
        foreach (float f in _posicoesTornozelo)
        {
            std += (f - average) * (f - average);
        }
        std = Mathf.Sqrt(std / _posicoesTornozelo.Count);
        var th = 2.5; // sensibilidade de deteção, quanto mais alto, mais alto é necessário saltar.
        var jump_threshold = average + th * std;
        return (ankleL > jump_threshold && ankleR > jump_threshold) ;
    }
    
    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }
    
    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }
}
