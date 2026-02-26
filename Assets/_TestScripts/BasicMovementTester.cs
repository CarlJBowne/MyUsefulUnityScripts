using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovementTester : MonoBehaviour
{
	Rigidbody2D rb;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate()
	{
		rb.linearVelocity = new(Input.GetAxis("Horizontal") * 10, Input.GetAxis("Vertical") * 10f);
	}
}
