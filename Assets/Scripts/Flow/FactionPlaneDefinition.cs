using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen.Flow
{
    /// <summary>
    /// Identificador numérico de las cuatro facciones beligerantes en el conflicto ucroniano de 1944.
    /// Incluye compatibilidad retrospectiva con los nombres conceptuales previos del prototipo.
    /// </summary>
    public enum GameFactionId
    {
        /// <summary>Insurrección anarquista clandestina con aviones ultraligeros de alta aceleración.</summary>
        Anarquistas = 0,
        /// <summary>Bloque corporativo y bursátil con monoplanos armados de alta tecnología y precisión.</summary>
        Capitalistas = 1,
        /// <summary>Unión soviética colectiva con acorazados aéreos pesados de acero macizo y blindaje extremo.</summary>
        Comunistas = 2,
        /// <summary>Régimen marcial autoritario con interceptores en punta de flecha diseñados para el ataque en picado.</summary>
        Fascistas = 3,

        // Compatibilidad histórica con enumeraciones previas del desarrollo
        VanguardiaPesada = 0,
        MonoplanoClandestino = 1,
        ConsorcioAgrario = 2,
        DoctrinaZenit = 3
    }

    /// <summary>
    /// Entidad de datos que modela la ficha técnica, estética, narrativa y operativa de cada avión de facción.
    /// Contiene los atributos mecánicos (1 al 10), paleta cromática, rutas a assets de interfaz y textos de cinemática.
    /// </summary>
    [System.Serializable]
    public class FactionPlaneDefinition
    {
        [Tooltip("Identificador formal de la facción política.")]
        public GameFactionId factionId;

        [Tooltip("Nombre visible en la interfaz (ej. ANARQUISTAS, CAPITALISTAS).")]
        public string factionName;

        [Tooltip("Denominación militar y modelo del caza (ej. «KÓLOKOL» VUELO LIBRE).")]
        public string planeCode;

        [Tooltip("Lema propagandístico o doctrina del bando.")]
        public string slogan;

        [TextArea(2, 4)]
        [Tooltip("Descripción del comportamiento balístico y filosofía de ingeniería del avión.")]
        public string mechanicalPhilosophy;

        [Header("Estadísticas de Nave (1 al 10)")]
        [Range(1f, 10f)]
        [Tooltip("Velocidad de avance y tasa de aceleración de la aeronave.")]
        public float speed = 8f;

        [Range(1f, 10f)]
        [Tooltip("Potencia destructiva por proyectil y cadencia de fuego.")]
        public float firepower = 7f;

        [Range(1f, 10f)]
        [Tooltip("Capacidad de absorción de daños de la armadura antes de la destrucción.")]
        public float armor = 6f;

        [Range(1f, 10f)]
        [Tooltip("Afinidad y margen de personalización mediante mejoras de taller.")]
        public float upgrades = 8f;

        // Propiedades de retrocompatibilidad con versiones previas del controlador de vuelo
        public float enginePower { get => speed; set => speed = value; }
        public float armorResistance { get => armor; set => armor = value; }
        public float agilityHandling { get => upgrades; set => upgrades = value; }
        public float fireDispersion { get => firepower; set => firepower = value; }

        [Header("Colores Visuales de Facción")]
        [Tooltip("Color primario para marcos, encabezados y barras de progreso.")]
        public Color primaryColor = new Color(0.85f, 0.25f, 0.25f);

        [Tooltip("Color de acento secundario para contrastes y detalles decorativos.")]
        public Color accentColor = new Color(1.0f, 0.85f, 0.4f);

        [Tooltip("Color de fondo atenuado para paneles y pantallas.")]
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.1f);

        [Header("Marco Visual / Interfaz de Facción")]
        [Tooltip("Ruta relativa al asset del marco completo de fondo.")]
        public string frameAssetPath;

        [Tooltip("Ruta al marco horizontal decorativo para el bloque de estadísticas.")]
        public string horizontalFramePath;

        [Tooltip("Ruta al marco vertical para encuadrar la silueta del caza.")]
        public string verticalFramePath;

        [Tooltip("Ruta al emblema o escudo de la facción.")]
        public string emblemAssetPath;

        // Referencias directas en memoria a los Sprites precargados
        public Sprite factionFrameSprite;
        public Sprite horizontalFrameSprite;
        public Sprite verticalFrameSprite;
        public Sprite factionEmblemSprite;

        [Header("Cinemática e Historia Ascendente")]
        [Tooltip("Párrafos narrativos que descienden durante la cinemática de historia.")]
        public List<string> cinematicParagraphs = new List<string>();

        [Tooltip("Nombres de escenarios o fondos que ilustran los acontecimientos.")]
        public List<string> backgroundMockNames = new List<string>();
    }

    /// <summary>
    /// Almacén estático y máquina de estado para la persistencia del flujo de juego (Game Flow) del jugador.
    /// Gestiona las preferencias de usuario guardadas en PlayerPrefs: si ha completado el tutorial
    /// y cuál facción tiene actualmente seleccionada para el menú principal.
    /// </summary>
    public static class GameFlowDataStore
    {
        // Claves persistentes en el registro de PlayerPrefs
        private const string PREF_INTRO_COMPLETED = "AK47_IntroCompleted";
        private const string PREF_SELECTED_FACTION = "AK47_SelectedFactionId";

        // Instancia en caché de la facción activa en memoria
        private static FactionPlaneDefinition _selectedFaction;

        /// <summary>
        /// Indica si el usuario ya vio la cinemática inicial y completó el tutorial con éxito.
        /// Si es true, el juego arranca directamente en el menú de la facción elegida.
        /// </summary>
        public static bool HasCompletedIntroFlow
        {
            get => PlayerPrefs.GetInt(PREF_INTRO_COMPLETED, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_INTRO_COMPLETED, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Obtiene o asigna el ID entero de la facción guardada en PlayerPrefs.
        /// </summary>
        public static int SavedFactionId
        {
            get => PlayerPrefs.GetInt(PREF_SELECTED_FACTION, (int)GameFactionId.Anarquistas);
            set
            {
                PlayerPrefs.SetInt(PREF_SELECTED_FACTION, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Devuelve la definición completa de la facción actualmente seleccionada por el jugador.
        /// Si aún no se ha cargado en memoria, la recupera desde PlayerPrefs o devuelve la predeterminada.
        /// </summary>
        public static FactionPlaneDefinition SelectedFaction
        {
            get
            {
                if (_selectedFaction == null)
                {
                    _selectedFaction = LoadSavedFaction();
                }
                return _selectedFaction;
            }
            set
            {
                _selectedFaction = value;
                if (value != null)
                {
                    SavedFactionId = (int)value.factionId;
                }
            }
        }

        /// <summary>
        /// Guarda de forma segura una facción en memoria y en almacenamiento permanente.
        /// </summary>
        /// <param name="faction">Definición de la facción elegida.</param>
        public static void SaveFaction(FactionPlaneDefinition faction)
        {
            _selectedFaction = faction;
            if (faction != null)
            {
                SavedFactionId = (int)faction.factionId;
            }
        }

        /// <summary>
        /// Marca el flujo inicial como completado. A partir de este momento,
        /// los siguientes arranques omitirán la cinemática y el tutorial.
        /// </summary>
        public static void MarkIntroFlowCompleted()
        {
            HasCompletedIntroFlow = true;
            Debug.Log("<color=green><b>[GameFlow]</b> Flujo inicial completado y guardado en PlayerPrefs. En los próximos inicios irá directo al Menú Principal.</color>");
        }

        /// <summary>
        /// Reinicia el progreso inicial del jugador para propósitos de prueba o depuración en el Editor.
        /// </summary>
        public static void ResetIntroFlow()
        {
            PlayerPrefs.DeleteKey(PREF_INTRO_COMPLETED);
            PlayerPrefs.DeleteKey(PREF_SELECTED_FACTION);
            PlayerPrefs.Save();
            _selectedFaction = GetAllFactions()[0];
            Debug.Log("<color=yellow><b>[GameFlow]</b> Flujo inicial reseteado. La próxima vez se mostrará de nuevo la Selección de Facción y el Tutorial.</color>");
        }

        /// <summary>
        /// Carga la facción almacenada en PlayerPrefs desde el catálogo de facciones.
        /// </summary>
        private static FactionPlaneDefinition LoadSavedFaction()
        {
            var factions = GetAllFactions();
            int id = SavedFactionId;
            foreach (var f in factions)
            {
                if ((int)f.factionId == id) return f;
            }
            return factions[0];
        }

        /// <summary>
        /// Catálogo maestro con las 4 facciones del conflicto de 1944:
        /// 1. Anarquistas: Caza «Kólokol» - Máxima maniobrabilidad y capacidad de modificación.
        /// 2. Capitalistas: Monoplano «Mercurio Corp-IX» - Precisión suiza, cadencia y rentabilidad.
        /// 3. Comunistas: Acorazado «Tempestad Proletaria T-34» - Blindaje pesado y fuego imparable.
        /// 4. Fascistas: Interceptor «Águila Marcial Falco-X» - Velocidad de picado y estética de choque.
        /// </summary>
        /// <returns>Lista inmutable con las cuatro definiciones de facción.</returns>
        public static List<FactionPlaneDefinition> GetAllFactions()
        {
            var list = new List<FactionPlaneDefinition>();

            // 1. ANARQUISTAS
            list.Add(new FactionPlaneDefinition
            {
                factionId = GameFactionId.Anarquistas,
                factionName = "ANARQUISTAS",
                planeCode = "«KÓLOKOL» VUELO LIBRE",
                slogan = "«Sin amos, sin fronteras: el cielo pertenece a quien lo desafía»",
                mechanicalPhilosophy = "INSTRUCCIONES DE VUELO: Caza táctico ultra-ligero de asalto guerrillero montado en talleres clandestinos. Cuenta con máxima aceleración y un chasis flexible pensado para la improvisación balística y mejoras continuas sobre la marcha, sacrificando espesor de blindaje.",
                speed = 9.5f,
                firepower = 7.0f,
                armor = 4.0f,
                upgrades = 9.8f,
                frameAssetPath = "Assets/Art/Interfaces/Anarquista/Anarquismo.png",
                horizontalFramePath = "Assets/Art/Interfaces/Anarquista/frame_horizontal_anarquista.png",
                verticalFramePath = "Assets/Art/Interfaces/Anarquista/frame_vertical_anarquista.png",
                primaryColor = new Color(0.9f, 0.2f, 0.2f), // Rojo insurgente
                accentColor = new Color(0.2f, 0.2f, 0.2f),  // Negro azabache
                backgroundColor = new Color(0.1f, 0.04f, 0.04f),
                cinematicParagraphs = new List<string>
                {
                    "EL CIELO NO TIENE FRONTERAS TRAZADAS POR GOBIERNOS NI BANQUEROS.",
                    "En los barracones abandonados del ferrocarril, los mecánicos desarmaron los motores confiscados del Estado.",
                    "No tenemos fábricas blindadas ni subsidios ministeriales, pero cada tuerca de tu caza ha sido limada por manos libres.",
                    "Cuando las escuadras de asalto levanten el vuelo, no habrá líderes ordenando masacres.",
                    "Tu velocidad es tu escudo, tu ingenio tu artillería.\nLa insurrección surca los vientos."
                },
                backgroundMockNames = new List<string> { "Taller Clandestino", "Pista de Tierra", "Torre de Señales Destruida" }
            });

            // 2. CAPITALISTAS
            list.Add(new FactionPlaneDefinition
            {
                factionId = GameFactionId.Capitalistas,
                factionName = "CAPITALISTAS",
                planeCode = "«MERCURIO CORP-IX»",
                slogan = "«La supremacía balística es el activo de inversión más rentable»",
                mechanicalPhilosophy = "INSTRUCCIONES DE VUELO: Monoplano militar de élite financiado por corporaciones transcontinentales. Incorpora cañones rotativos de alta cadencia de disparo y turbocompresores de precisión suiza. Ofrece un equilibrio perfecto entre potencia de fuego y tecnología de mejoras avanzadas.",
                speed = 8.8f,
                firepower = 9.2f,
                armor = 6.0f,
                upgrades = 8.5f,
                frameAssetPath = "Assets/Art/Interfaces/Capitalista/Capitalismo.png",
                horizontalFramePath = "Assets/Art/Interfaces/Capitalista/frame_horizontal_capitalista.png",
                verticalFramePath = "Assets/Art/Interfaces/Capitalista/frame_vertical_capitalista.png",
                primaryColor = new Color(0.85f, 0.72f, 0.25f), // Oro bursátil
                accentColor = new Color(0.15f, 0.35f, 0.6f),   // Azul financiero
                backgroundColor = new Color(0.04f, 0.07f, 0.12f),
                cinematicParagraphs = new List<string>
                {
                    "TODO CONFLICTO ES UNA OPORTUNIDAD DE EXPANSIÓN COMERCIAL.",
                    "Los accionistas de la junta directiva han liquidado los bonos de deuda agraria.",
                    "Tu contrato de vuelo garantiza el pago en oro puro por cada batería enemiga neutralizada.",
                    "Nuestros cazas no son simples máquinas: son la vanguardia del libre mercado aerotransportado.",
                    "Asegura la ruta aérea de suministro.\nEl progreso tiene precio y nosotros somos los dueños del pago."
                },
                backgroundMockNames = new List<string> { "Bolsa de Valores", "Hangar Corporativo", "Cielo Metropolitano" }
            });

            // 3. COMUNISTAS
            list.Add(new FactionPlaneDefinition
            {
                factionId = GameFactionId.Comunistas,
                factionName = "COMUNISTAS",
                planeCode = "«TEMPESTAD PROLETARIA T-34»",
                slogan = "«El yunque forja la coraza; el martillo aplasta la reacción»",
                mechanicalPhilosophy = "INSTRUCCIONES DE VUELO: Fortaleza aérea construida en masa con acero fundido en los altos hornos de los Urales. Blindaje reforzado de máxima resistencia y cañones frontales devastadores diseñados para el avance imparable en formaciones de choque.",
                speed = 5.5f,
                firepower = 9.6f,
                armor = 9.8f,
                upgrades = 6.0f,
                frameAssetPath = "Assets/Art/Interfaces/Comunista/Comunismo.png",
                horizontalFramePath = "Assets/Art/Interfaces/Comunista/frame_horizontal_comunismo.png",
                verticalFramePath = "Assets/Art/Interfaces/Comunista/frame_vertical_comunista.png",
                primaryColor = new Color(0.8f, 0.12f, 0.12f), // Rojo escarlata
                accentColor = new Color(1.0f, 0.85f, 0.2f),   // Amarillo soviético
                backgroundColor = new Color(0.12f, 0.04f, 0.04f),
                cinematicParagraphs = new List<string>
                {
                    "EL ACERO DEL PUEBLO RESISTIRÁ CUALQUIER TEMPESTAD.",
                    "Desde las calderas de la fundición hasta los ensambladores de fuselajes, millones de obreros forjaron tu armadura.",
                    "Los generales pretendieron doblegarnos mediante el hambre y el bloqueo de carburante.",
                    "Ahora, la flota de choque proletaria despega como una sola muralla roja sobre el frente.",
                    "¡Ni un paso atrás!\nLa victoria del trabajo sobre el capital se decide en los cielos."
                },
                backgroundMockNames = new List<string> { "Fábrica de Tractores", "Monumento de Cemento", "Frente de Batalla" }
            });

            // 4. FASCISTAS
            list.Add(new FactionPlaneDefinition
            {
                factionId = GameFactionId.Fascistas,
                factionName = "FASCISTAS",
                planeCode = "«ÁGUILA MARCIAL FALCO-X»",
                slogan = "«Orden absoluto, disciplina ciega y fuego implacable desde las alturas»",
                mechanicalPhilosophy = "INSTRUCCIONES DE VUELO: Interceptor militar de ataque en picado con fuselaje en punta de flecha y geometría agresiva. Excelente velocidad de intercepción y blindaje reforzado, diseñado para la intimidación psicológica y el derribo sistemático.",
                speed = 9.0f,
                firepower = 8.5f,
                armor = 8.2f,
                upgrades = 5.0f,
                frameAssetPath = "Assets/Art/Interfaces/Facista/Facsimo.png",
                horizontalFramePath = "Assets/Art/Interfaces/Facista/frame_horizontal_facista.png",
                verticalFramePath = "Assets/Art/Interfaces/Facista/frame_vertical_facista.png",
                primaryColor = new Color(0.3f, 0.32f, 0.35f), // Gris acorazado
                accentColor = new Color(0.95f, 0.55f, 0.1f),  // Ámbar de guerra
                backgroundColor = new Color(0.06f, 0.06f, 0.07f),
                cinematicParagraphs = new List<string>
                {
                    "LA DISCIPLINA ES LA ÚNICA LEY QUE RECONOCEN LAS ALTURAS.",
                    "El Estado exige sumisión absoluta y entrega total de cada piloto a la gloria nacional.",
                    "Tu juramento fue sellado ante las banderas de acero y el águila imperial.",
                    "No hay espacio para la duda ni la compasión en la doctrina de conquista aérea.",
                    "El motor aúlla con furia implacable.\nHaz descender el juicio de fuego sobre los insurgentes."
                },
                backgroundMockNames = new List<string> { "Desfile Militar", "Fortaleza Costera", "Formación de Cazas" }
            });

            return list;
        }
    }
}
