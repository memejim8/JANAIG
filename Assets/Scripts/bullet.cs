using System.Collections;
using TMPro;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Debug")]
    // Debug
    public bool debug = false;
    public bool debugSwitch = false;

    [Header("Function Debug")]
    // Function Debugging
    public bool startthrow = false;
    public bool startcolorchange = false;
    void Update() 
    {
        if (debug)
        {
            if (startthrow) { startthrow = false; Debug.Log("Debug Throw Start!"); RestartThrow(); } 
            if (startcolorchange) { startcolorchange = false; Debug.Log("Debug Color Change Start!"); ChangeColorAndText(); }
        }
    }

    [Header("Dummy Mode")]
    public bool dummy_mode = true;
    public bool dummy_turn = true;
    public float dummy_wait_time = 0f;
    public float DummyHP = 0;
    public float DummyStock = 0;
    public TextMeshPro scoreText, HPText;

    [Header("Throw Settings")]
    // Throw variation settings
    public Material ThrowDirectionMaterial;
    public VelocityEstimator velocityEstimator;
    public Vector2 swordVelocity2D;
    public Vector3 swordVelocity3D;
    public float ThrowDistanceScalar = 4f;
    public float throwVariationX = 5f;
    public float throwVariationY = 10f;
    public float throwVariationYZ = 4f;
    public float throwVariationZ = 15f;


    [Header("Health and Speed")]
    // Current speed and health of the bullet
    public float currentSP = 0f;
    public float currentHP = 0f;
    public Material bulletMaterial, WatchMaterial, trailMaterial;
    public bool SeparateColorMode = false;

    [Header("Freeze Settings")]
    // Flag to determine if the bullet is frozen
    public bool isFrozen = false;
    // Duration of freezing time
    public float frozenTime = 1.0f;

    // Store the name of the previously collided object
    private string previousCollidedObjectName = "";

    private Rigidbody rb;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        // Get the Rigidbody component of the bullet
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
        ChangeColorAndText();
        ThrowDirectionMaterial.SetFloat("_angle", 0);
    }

    public void CollideWithSword(Collider swordCollider)
    {
        gameObject.GetComponent<MoveAndGrowBullet>().MoveAndGrowBulletFunction();
        ChangeColorAndText();

        if (swordCollider.gameObject.CompareTag("Weapon"))
        {
            velocityEstimator = swordCollider.gameObject.GetComponent<SliceObject>().velocityEstimator;
            swordVelocity3D = velocityEstimator.GetVelocityEstimate();
            swordVelocity2D = new Vector2(swordVelocity3D.z, swordVelocity3D.y);

            float angle = Vector2.Angle
            (
                new Vector2(0,1),
                swordVelocity2D.normalized
            );

            Debug.Log("Cut angle is: " + angle);
            ThrowDirectionMaterial.SetFloat("_angle", -angle);

            if (!isFrozen)
            {
                isFrozen = true;
                StopAllCoroutines();
                if (dummy_mode) { dummy_turn = false; }
                // Wait for frozen time before throwing again
                Debug.Log("First Hit!");
                StartCoroutine(WaitThenThrow());
                // Store the name of the collided object
                previousCollidedObjectName = GetParentName(swordCollider);
            }
            else
            {
                // Get the name of the currently collided object
                string currentCollidedObjectName = GetParentName(swordCollider);
                if (previousCollidedObjectName != currentCollidedObjectName)
                {
                    // Update previous object name and adjust health and speed
                    previousCollidedObjectName = currentCollidedObjectName;

                    (float hp, float speed) = GetHPAndSpeed(swordCollider);
                    currentHP += hp;
                    //Debug.Log("Speed Add!");
                    currentSP += speed;
                }
                else
                {
                    // Reset previous object name and initiate throw
                    previousCollidedObjectName = "";
                    RestartThrow();
                }
            }
        }
        
    }

    private IEnumerator WaitThenThrow()
    {
        Debug.Log("Waiter Activated!");
        // Wait for frozen time before initiating throw
        yield return new WaitForSeconds(frozenTime);
        Debug.Log("Waiter Finished");
        if (dummy_mode) { dummy_turn = false; }
        if (isFrozen) RestartThrow();
    }

    private IEnumerator Throw()
    {  
        if ( dummy_turn && dummy_mode )
        {
            trailRenderer.emitting = false;
            yield return new WaitForSeconds(0.1f);
            rb.MovePosition(new Vector3(-10f,1f,0f));
            yield return new WaitForSeconds(0.1f);
            trailRenderer.emitting = true;
            yield return new WaitForSeconds(dummy_wait_time); 
        }

        isFrozen = false;

        // Calculate throw duration based on current speed
        float throwDuration = -0.005f * currentSP + 3f;

        // Define throw trajectory points
        Vector3 PlayerPos = new Vector3(10f, 1.5f, 0f);
        Vector3 DummyPos = new Vector3(-10f, 1f, 0f);

        Vector3 startPos = gameObject.transform.position;
        Vector3 endPos = DummyPos;

        if (dummy_turn) //if its dummy's turn the bullet goes from the dummy to the player
        {
            startPos = DummyPos;
            endPos = PlayerPos;
        }

        // Define control point for Bezier curve
        Vector2 swordVelocity2DRandomized = swordVelocity3D.magnitude * ThrowDistanceScalar * swordVelocity2D;
        Vector3 controlPos = new Vector3(
            Random.Range(-throwVariationX, throwVariationX),
            swordVelocity2DRandomized.y,
            swordVelocity2DRandomized.x
        );

        //Debug.Log("Throw at " + controlPos);

        float elapsedTime = 0.0f;
        while (elapsedTime < throwDuration && !isFrozen)
        {

            float t = elapsedTime / throwDuration;

            // Calculate new position using Quadratic Bezier curve formula
            Vector3 newPos = Mathf.Pow(1 - t, 2) * startPos + 2 * (1 - t) * t * controlPos + Mathf.Pow(t, 2) * endPos;

            // Move the bullet to the new position
            rb.MovePosition(newPos);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //Debug.Log("Miss");

        // if it was players turn before, increase score and stock
        if (!dummy_turn)
        {
            currentSP = 0;
            DummyHP += currentHP;
            currentHP = 0;
            if (DummyHP >= 400)
            {
                DummyHP = 0;
                DummyStock += 1;
            }
            //Debug.Log("Hit! Current HP: " + DummyHP + "Current Stock: " + DummyStock); 
        }
        else
        {
            // if player missed, then reset the score
            (DummyHP, DummyStock) = (0, 0);
        }
        scoreText.text = DummyStock.ToString();
        HPText.text = DummyHP.ToString();
        ChangeColorAndText();
        // prepare for dummy rethrow
        dummy_turn = true;
        RestartThrow();
    }

    private void RestartThrow()
    {
        gameObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        StopAllCoroutines();
        StartCoroutine(Throw());
    }

    private string GetParentName(Collider swordCollider)
    {
        // Get the name of the parent object if it exists
        Transform parentTransform = swordCollider.transform.parent;
        return parentTransform != null ? parentTransform.gameObject.name : "";
    }

    private (float, float) GetHPAndSpeed(Collider swordCollider)
    {
        // Get health and speed values from the collided object's WeaponSelector component
        WeaponSelector wp = swordCollider.transform.parent.GetComponent<WeaponSelector>();
        return wp != null ? (wp.HitHP, wp.HitSP) : (0f, 0f);
    }

    private void ChangeColorAndText()
    {   
        if (currentHP > 400) { currentHP = 400; }
        if (currentSP > 400) { currentSP = 400; }

        scoreText.text = DummyStock.ToString();
        HPText.text = DummyHP.ToString();
 
        WatchMaterial.SetFloat("_SP", currentSP/400);
        WatchMaterial.SetFloat("_HP", currentHP/400);

        trailMaterial.SetFloat("_SP", currentSP/400);
        trailMaterial.SetFloat("_HP", currentHP/400);

        bulletMaterial.SetFloat("_SP", currentSP/400);
        bulletMaterial.SetFloat("_HP", currentHP/400);
    }
}
