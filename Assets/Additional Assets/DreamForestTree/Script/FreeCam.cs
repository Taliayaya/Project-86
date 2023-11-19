using UnityEngine;
using System.Collections;

public class FreeCam : MonoBehaviour 
{
    public float speedNormal = 3f;
    public float speedFast = 45f;
    public float mouseSensX = 2f;
    public float mouseSensY = 2f;
    float rotY;
    float speed;
    
	void Start()
	{
        //Cursor.visible = false;
	}
   
	void Update()
	{
        if (Input.GetMouseButton(0))
        {
            //Cursor.visible = true;
        }
        
        if (Input.GetMouseButton(1)) 
        {
            float rotX = transform.localEulerAngles.y + (Input.GetAxis("Mouse X") * mouseSensX);
            rotY += Input.GetAxis("Mouse Y") * mouseSensY;
            rotY = Mathf.Clamp(rotY, -80f, 80f);
            transform.localEulerAngles = new Vector3(-rotY, rotX, 0f);
            //Cursor.visible = false;
        }
        float forward = Input.GetAxis("Vertical");
        float side  = Input.GetAxis("Horizontal");
        if (forward != 0f)  
        {
            if (Input.GetKey(KeyCode.LeftShift)) speed = speedFast;
            else speed = speedNormal;
            Vector3 vect = new Vector3(0f, 0f, forward * speed * Time.deltaTime);
            transform.localPosition += transform.localRotation * vect;
        }
        if (side != 0f) 
        {
            if (Input.GetKey(KeyCode.LeftShift)) speed = speedFast;
            else speed = speedNormal;
            Vector3 vect = new Vector3(side * speed * Time.deltaTime, 0f, 0f);
            transform.localPosition += transform.localRotation * vect;
        }
	}
}
