using UnityEngine;
using UnityEngine.AI;

public class AgenteIA : MonoBehaviour
{
    public enum Estado
    {
        Patrullando,
        Investigando,
        Persiguiendo
    }

    [Header("Referencias")]
    public Transform jugador;
    public ControlJugador jugadorScript;

    [Header("Visión")]
    public float distanciaVision = 12f;
    public float anguloVision = 60f;
    public LayerMask capasVision;

    [Header("Audición")]
    public float distanciaMaxEscucha = 15f;

    [Header("Velocidades")]
    public float velocidadPatrulla = 3.5f;
    public float velocidadInvestigacion = 4.5f;
    public float velocidadPersecucion = 6f;

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    private int indiceActual = 0;

    private Vector3 ultimaPosicionOida;
    private float temporizadorInvestigacion = 0f;
    public float tiempoEsperaInvestigacion = 3f;

    private bool estaMirando = false;
    private float velocidadGiro = 60f; // grados por segundo
    private float anguloMaximo = 60f;
    private float anguloActual = 0f;
    private float direccionGiro = 1f;
    private Quaternion rotacionInicial;

    private Vector3 ultimaPosicionVista;




    private NavMeshAgent agent;
    private Estado estadoActual;
    private Animator animator;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        estadoActual = Estado.Patrullando;
        agent.speed = velocidadPatrulla;
        IrAlSiguientePunto();
    }

    void Update()
    {
        if (jugador == null || jugadorScript == null)
        {
            Patrullar();
            return;
        }

        bool veJugador = ComprobarVision();
        bool oyeJugador = ComprobarAudicion();

        switch (estadoActual)
        {
            case Estado.Patrullando:

                if (veJugador)
                {
                    estadoActual = Estado.Persiguiendo;
                }
                else if (oyeJugador)
                {
                    estadoActual = Estado.Investigando;
                    ultimaPosicionOida = jugador.position;
                    temporizadorInvestigacion = tiempoEsperaInvestigacion;
                    agent.SetDestination(ultimaPosicionOida);
                }

                break;

            case Estado.Investigando:

                if (veJugador)
                {
                    estadoActual = Estado.Persiguiendo;
                    agent.isStopped = false;
                    estaMirando = false;
                    break;
                }

                // Si todavía no llegó al punto, sigue moviéndose
                if (!agent.pathPending &&
                    agent.remainingDistance > agent.stoppingDistance)
                {
                    break;
                }

                // Ha llegado → empieza fase de mirar
                if (!estaMirando)
                {
                    agent.isStopped = true;
                    estaMirando = true;
                    rotacionInicial = transform.rotation;
                }

                // Rotación izquierda-derecha
                anguloActual += velocidadGiro * direccionGiro * Time.deltaTime;

                if (Mathf.Abs(anguloActual) >= anguloMaximo)
                    direccionGiro *= -1f;

                transform.rotation = rotacionInicial * Quaternion.Euler(0, anguloActual, 0);

                temporizadorInvestigacion -= Time.deltaTime;

                if (temporizadorInvestigacion <= 0f)
                {
                    agent.isStopped = false;
                    estaMirando = false;
                    estadoActual = Estado.Patrullando;
                    IrAlSiguientePunto();
                }

                break;


            case Estado.Persiguiendo:

                if (veJugador)
                {
                    ultimaPosicionVista = jugador.position;
                    agent.SetDestination(jugador.position);
                }
                else
                {
                    // Pierde visión → pasa a investigar última posición vista
                    estadoActual = Estado.Investigando;
                    ultimaPosicionOida = ultimaPosicionVista;
                    temporizadorInvestigacion = tiempoEsperaInvestigacion;
                    agent.SetDestination(ultimaPosicionOida);
                }

                break;

        }

        EjecutarEstado();
        ActualizarAnimaciones();
    }


    void EjecutarEstado()
    {
        switch (estadoActual)
        {
            case Estado.Patrullando:
                agent.speed = velocidadPatrulla;
                Patrullar();
                break;

            case Estado.Investigando:
                agent.speed = velocidadInvestigacion;
                break;

            case Estado.Persiguiendo:
                agent.speed = velocidadPersecucion;
                break;
        }
    }


    void Patrullar()
    {
        if (puntosPatrulla.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            indiceActual = (indiceActual + 1) % puntosPatrulla.Length;
            IrAlSiguientePunto();
        }
    }


    void IrAlSiguientePunto()
    {
        if (puntosPatrulla.Length == 0) return;
        agent.SetDestination(puntosPatrulla[indiceActual].position);
    }

    bool ComprobarVision()
    {
        Vector3 direccion = (jugador.position - transform.position).normalized;
        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia > distanciaVision)
            return false;

        float angulo = Vector3.Angle(transform.forward, direccion);
        if (angulo > anguloVision / 2f)
            return false;

        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, direccion);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaVision, capasVision))
        {
            if (hit.transform == jugador)
                return true;
        }

        return false;
    }

    bool ComprobarAudicion()
    {
        float distancia = Vector3.Distance(transform.position, jugador.position);
        float ruido = jugadorScript.ObtenerNivelRuido();

        return ruido > 0f && distancia < ruido && distancia < distanciaMaxEscucha;
    }

    void ActualizarAnimaciones()
    {
        float velocidadActual = agent.velocity.magnitude;

        float velocidadNormalizada = velocidadActual / velocidadPersecucion;

        animator.SetFloat("Speed", velocidadNormalizada, 0.1f, Time.deltaTime);
    }

}