using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Pistola : MonoBehaviour
{
    [Header("Referencias")]
    public Transform firePoint;
    public LineRenderer line;
    public GameObject fx1, fx2;

    private XRGrabInteractable grabInteract;

    void Start()
    {
        grabInteract = GetComponent<XRGrabInteractable>();

        grabInteract.activated.AddListener(x => OnActivated());
    }

    private void OnActivated()
    {
        StartCoroutine(Disparando());
    }

    IEnumerator Disparando()
    {
        RaycastHit hitInfo;

        Instantiate(fx1, firePoint.position, firePoint.rotation);

        if (Physics.Raycast(firePoint.position, firePoint.forward, out hitInfo))
        {
            Debug.Log(hitInfo.collider.name);

            line.SetPosition(0, firePoint.position);
            line.SetPosition(1, hitInfo.point);
            Instantiate(fx2, hitInfo.point, hitInfo.collider.gameObject.transform.rotation);
        }
        else
        {
            line.SetPosition(0, firePoint.position);
            line.SetPosition(1, firePoint.position + firePoint.forward * 30f);
        }

        line.enabled = true;

        yield return new WaitForSeconds(0.02f);

        line.enabled = false;
    }
}