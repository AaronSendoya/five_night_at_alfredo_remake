using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Teléfono de la oficina (obj_telefono): en la noche 1 suena a los 2 s; al contestar se oye
    /// el mensaje con subtítulos. Corrige el original, que se podía contestar desde 2000 px.
    /// </summary>
    public class PhoneCall : Interactable
    {
        [SerializeField] Transform visual;
        [SerializeField] float ringDelay = 2f;
        [SerializeField] Text subtitleText;
        [SerializeField] GameObject subtitlePanel;

        static readonly string[] Lines =
        {
            "Hola? Hola? Oh, espero que se me escuche",
            "Bien, tu primera noche, supongo que ya leíste el manual",
            "y sabes las cosas básicas, pero los que ya trabajamos aquí",
            "sabemos un par de cosas sobre las que... hay que tener cuidado,",
            "nada muy peligroso pero es mejor saberlas, ¿sabes? Jejej",
            "ehhh bueno, los animatrónicos tienen una inteligencia artificial",
            "que hace que se muevan, es lo que hace que bailen y actúen en el día,",
            "lo curioso es que también salen a estirar las piernas en la noche,",
            "solo pasean por la pizzería pero... no hacen nada raro,",
            "la mayoría de las veces jeje, bueno en cualquier caso",
            "es mejor que no te les acerques, en ningún caso,",
            "ni mientras caminan ni mientras bailan,",
            "a veces son algo... impredecibles y debido a su programación",
            "podrían confundirte con un endoesqueleto sin traje",
            "y tratarán de meterte en uno con un poco de su 'fuerza de robot', jeje,",
            "eso no será nada bueno así que solo mantente fuera de su alcance, ¿ok?",
            "Ah, como podrás ver la pizzería es algo vieja",
            "y hay cosas que dejan de funcionar de repente,",
            "no querrás quedarte a oscuras con esas cosas rondando por ahí,",
            "tienen muy mala visión pero en la noche tú tampoco verás mucho",
            "así que lo mejor es que te encargues de cambiar los fusibles cuando eso ocurra",
            "Yyyy creo que eso es todo, buena suerte,",
            "te irá bien mientras sigas los consejos, adiós"
        };
        static readonly float[] Times = { 0, 5, 10, 15, 19, 24, 29, 33, 37, 41, 45, 49, 53, 57, 61, 65, 69, 73, 77, 81, 85, 89, 94 };
        const float CallLength = 97f;

        enum State { Waiting, Ringing, Talking, Done }
        State state = State.Waiting;
        float timer;
        Vector3 basePos;

        void Start()
        {
            if (visual != null) basePos = visual.localPosition;
            if (GameManager.Instance.Night.night != 1) state = State.Done;
            subtitlePanel.SetActive(false);
        }

        public override bool CanInteract(PlayerController player) => state == State.Ringing;
        public override bool IsTargetable(PlayerController player) => state == State.Ringing;
        public override InteractIcon Icon => InteractIcon.Phone;
        public override string GetPrompt(PlayerController player) => state == State.Ringing ? "Contestar el teléfono" : "Teléfono";

        public override void Interact(PlayerController player)
        {
            if (state != State.Ringing) return;
            AudioManager.StopLoop(Sfx.PhoneRing);
            AudioManager.PlayLoop(Sfx.PhoneVoice, false);
            state = State.Talking;
            timer = 0f;
            subtitlePanel.SetActive(true);
            if (visual != null) visual.localPosition = basePos;
        }

        public void SkipCall()
        {
            if (state != State.Talking) return;
            AudioManager.StopLoop(Sfx.PhoneVoice);
            Finish();
        }

        void Finish()
        {
            state = State.Done;
            subtitlePanel.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            switch (state)
            {
                case State.Waiting:
                    if (gm.State != GameState.Playing) return;
                    timer += Time.deltaTime;
                    if (timer >= ringDelay)
                    {
                        state = State.Ringing;
                        AudioManager.PlayLoop(Sfx.PhoneRing);
                        HudMessages.Show("Está sonando el teléfono de la oficina");
                    }
                    break;
                case State.Ringing:
                    if (visual != null) visual.localPosition = basePos + (Vector3)(Random.insideUnitCircle * 0.06f);
                    if (gm.State != GameState.Playing || gm.IsPaused) AudioManager.StopLoop(Sfx.PhoneRing);
                    else AudioManager.PlayLoop(Sfx.PhoneRing);
                    break;
                case State.Talking:
                    if (gm.State == GameState.Caught) { AudioManager.StopLoop(Sfx.PhoneVoice); Finish(); return; }
                    if (gm.IsPaused) return;
                    timer += Time.deltaTime;
                    int line = 0;
                    while (line + 1 < Times.Length && timer >= Times[line + 1]) line++;
                    subtitleText.text = Lines[line];
                    if (timer >= CallLength) { AudioManager.StopLoop(Sfx.PhoneVoice); Finish(); }
                    break;
            }
        }
    }
}
