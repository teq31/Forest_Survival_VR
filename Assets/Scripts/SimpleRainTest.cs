using UnityEngine;

public class SimpleRainTest : MonoBehaviour
{
    [Header("Trage aici obiectul Rainy VFX")]
    public GameObject rainGameObject;

    [Header("Manual Control")]
    public bool forceRainOn = true;

    private bool _lastState;

    void Start()
    {
        if (rainGameObject == null)
        {
            Debug.LogError("NU AI TRAS OBIECTUL DE PLOAIE IN SCRIPT!");
            return;
        }
        
        // Aplicam starea initiala
        ApplyState();
    }

    void Update()
    {
        if (rainGameObject == null) return;

        // Daca bifezi/debifezi casuta in timp ce te joci, se actualizeaza instant
        if (forceRainOn != _lastState)
        {
            ApplyState();
        }
    }

    void ApplyState()
    {
        _lastState = forceRainOn;
        rainGameObject.SetActive(forceRainOn);
        
        Debug.Log($"[TEST] Obiect Ploaie este acum: {(forceRainOn ? "ACTIV (ON)" : "INACTIV (OFF)")}");
        
        // Debug suplimentar: Verificam daca sistemul chiar merge
        if (forceRainOn)
        {
            var ps = rainGameObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (ps.isStopped) ps.Play();
                Debug.Log($"[TEST] ParticleSystem status: Playing? {ps.isPlaying} | Emitting? {ps.emission.enabled} | Count: {ps.particleCount}");
            }
        }
    }
    
    void OnGUI()
    {
        // Buton mare pe ecran sa testam fara Inspector
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 25;

        if (GUI.Button(new Rect(10, 200, 200, 80), forceRainOn ? "OPRESTE PLOAIA" : "PORNESTE PLOAIA"))
        {
            forceRainOn = !forceRainOn;
        }
    }
}