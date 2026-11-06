using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen.UI
{
    /// <summary>
    /// Gestor central persistente de la Hemeroteca militar.
    /// Permite a cualquier script de combate o GameManager registrar resultados de misiones y alimentar
    /// automáticamente el periódico histórico ("El Diario Montañés") con ecuaciones dinámicas.
    /// Persiste entre cambios de escena (gameplay -> menu).
    /// </summary>
    public static class HemerotecaManager
    {
        private const string PREFS_KEY = "Hemeroteca_MissionReports_JSON";

        [System.Serializable]
        private class ReportListWrapper
        {
            public List<MissionReportData> items = new List<MissionReportData>();
        }

        private static List<MissionReportData> cachedReports = null;
        public static event Action OnReportsUpdated;

        /// <summary>
        /// Obtiene todas las misiones registradas (cargadas de PlayerPrefs o memoria).
        /// </summary>
        public static List<MissionReportData> GetReports()
        {
            if (cachedReports == null || cachedReports.Count == 0)
            {
                LoadFromDisk();
            }
            if (cachedReports == null || cachedReports.Count == 0)
            {
                PopulateDefaultReports();
            }
            return new List<MissionReportData>(cachedReports);
        }

        private static void PopulateDefaultReports()
        {
            cachedReports = new List<MissionReportData>
            {
                new MissionReportData
                {
                    missionId = "MISIÓN 89-3",
                    missionName = "Asalto al Estuario de Hierro",
                    sectorName = "ARCHIPIÉLAGO CENTRAL",
                    isVictory = true,
                    completionPercentage = 94f,
                    accuracyPercentage = 84f,
                    enemiesDestroyed = 16,
                    alliedCasualties = 2,
                    civilianImpact = "Refugios y pistas a salvo al 100%",
                    warReason = "Asegurar el control de la refinería costera y quebrar la línea de radar enemiga.",
                    date = "14 DE OCTUBRE, 1944",
                    year = "1944",
                    edition = "EDICIÓN N° 47",
                    overrideHeadline = "¡VICTORIA EN EL ESTUARIO: 16 CAZAS DERRIBADOS!",
                    overrideSubtitle = "La vanguardia aérea aliada neutraliza los radares enemigos tras feroz combate en la niebla.",
                    overrideStory = "En una incursión al filo del amanecer, los cazas rompieron el bloqueo del canal costero. Tras un intenso intercambio de fuego a baja cota, los depósitos de combustible quedaron asegurados bajo nuestro pabellón.",
                    photoCaption = "Formación de combate en patrulla sobre los cañones marítimos.",
                    stampText = "CLASIFICADO // ESTADO MAYOR",
                    ad1Title = "TALLER RADIAL",
                    ad1Text = "Ajuste de bujías y hélices de paso variable.",
                    ad2Title = "VAPORES DE VUELO",
                    ad2Text = "Combustible de alto octanaje para monoplanos.",
                    ad3Title = "CHATARRA BÉLICA",
                    ad3Text = "Compramos restos de duraluminio al mejor precio."
                },
                new MissionReportData
                {
                    missionId = "MISIÓN 86-1",
                    missionName = "Emboscada en el Estuario",
                    sectorName = "BAHÍA DE LOS HEREJES",
                    isVictory = true,
                    completionPercentage = 100f,
                    accuracyPercentage = 91f,
                    enemiesDestroyed = 22,
                    alliedCasualties = 1,
                    civilianImpact = "Instalaciones portuarias protegidas",
                    warReason = "Neutralización de baterías antiaéreas pesadas en los islotes exteriores.",
                    date = "10 DE OCTUBRE, 1944",
                    year = "1944",
                    edition = "EDICIÓN N° 46",
                    overrideHeadline = "BARRIDO AÉREO COMPLETO EN LA BAHÍA EXTERIOR",
                    overrideSubtitle = "Las baterías costeras enemigas guardan silencio definitivo.",
                    overrideStory = "Nuestras escuadrillas barrieron las posiciones fortificadas enemigas en una maniobra envolvente. Ninguna batería enemiga logró abrir fuego a tiempo.",
                    photoCaption = "Impacto certero sobre las casamatas enemigas.",
                    stampText = "INFORME VERIFICADO",
                    ad1Title = "ARMERÍA PESADA",
                    ad1Text = "Munición trazadora calibre .50 en stock.",
                    ad2Title = "CASINO DE OFICIALES",
                    ad2Text = "Ruleta y dados abiertos tras el toque de queda.",
                    ad3Title = "LENTES DE PILOTO",
                    ad3Text = "Cristal polarizado militar antirreflejos."
                },
                new MissionReportData
                {
                    missionId = "MISIÓN 92-2",
                    missionName = "Defensa de la Refinería Central",
                    sectorName = "SECTOR PETROLERO NORTE",
                    isVictory = false,
                    completionPercentage = 68f,
                    accuracyPercentage = 75f,
                    enemiesDestroyed = 11,
                    alliedCasualties = 4,
                    civilianImpact = "Daños moderados en tanques secundarios de carburante",
                    warReason = "Incursión masiva enemiga buscando paralizar el abastecimiento.",
                    date = "4 DE OCTUBRE, 1944",
                    year = "1944",
                    edition = "EDICIÓN N° 45",
                    overrideHeadline = "FEROZ RESISTENCIA EN LA REFINERÍA BAJO FUEGO ENEMIGO",
                    overrideSubtitle = "Pese a las pérdidas sufridas, el complejo principal permanece operativo.",
                    overrideStory = "Una oleada sorpresa enemiga atacó con ventaja de altura. Nuestros pilotos trabaron combate en condiciones críticas, salvando los tanques principales antes de replegarse.",
                    photoCaption = "Humo sobre los depósitos tras el ataque.",
                    stampText = "URGENTE // ALERTA MÁXIMA",
                    ad1Title = "REPARACIONES",
                    ad1Text = "Remachado de chapa y parcheo de fuselajes.",
                    ad2Title = "BARRACÓN SOCIAL",
                    ad2Text = "Raciones de café negro y tabaco de pipa.",
                    ad3Title = "MAPAS TÁCTICOS",
                    ad3Text = "Cartografía naval actualizada disponible."
                }
            };
        }

        /// <summary>
        /// Registra el resultado de una misión completada o fallida.
        /// Automáticamente actualiza el periódico y lo coloca en la portada de la Hemeroteca.
        /// </summary>
        public static void RecordMission(MissionReportData data)
        {
            if (cachedReports == null) LoadFromDisk();

            // Buscar si ya existe la misión para actualizarla o añadirla como la más reciente
            int idx = cachedReports.FindIndex(m => m.missionId == data.missionId);
            if (idx >= 0)
            {
                cachedReports[idx] = data;
            }
            else
            {
                // La nueva misión se coloca en primera posición (portada del periódico más reciente)
                cachedReports.Insert(0, data);
            }

            SaveToDisk();
            OnReportsUpdated?.Invoke();
            Debug.Log($"<color=green><b>[HemerotecaManager]</b> Nueva crónica de guerra registrada para '{data.missionName}' ({data.sectorName}). Victoria: {data.isVictory} ({data.completionPercentage:F0}%).</color>");
        }

        /// <summary>
        /// Método rápido para invocar desde scripts de combate al terminar una partida.
        /// Aplica las ecuaciones militares para bajas, titulares y causas del conflicto.
        /// </summary>
        /// <param name="missionId">Identificador (ej. M-01).</param>
        /// <param name="sectorName">Nombre del sector (ej. ARCHIPIÉLAGO CENTRAL).</param>
        /// <param name="isVictory">¿Victoria aliada?</param>
        /// <param name="completionPercentage">Porcentaje completado (0 a 100).</param>
        /// <param name="enemiesDestroyed">Aviones enemigos abatidos.</param>
        /// <param name="alliedCasualties">Cazas propios derribados.</param>
        /// <param name="accuracy">Porcentaje de puntería (0 a 100).</param>
        /// <param name="warReason">Causa del conflicto bélico en ese sector.</param>
        public static void RecordCombatResult(string missionId, string sectorName, bool isVictory, float completionPercentage, int enemiesDestroyed, int alliedCasualties, float accuracy = 80f, string warReason = "")
        {
            if (string.IsNullOrEmpty(warReason))
            {
                warReason = isVictory 
                    ? $"Ofensiva táctica para asegurar los depósitos de combustible y neutralizar los aeródromos enemigos en {sectorName}."
                    : $"Incursión enemiga a gran escala buscando quebrar las defensas aéreas y aislar el {sectorName}.";
            }

            string todayDate = $"{DateTime.Now.Day} DE NOVIEMBRE, 1944";

            var report = new MissionReportData
            {
                missionId = missionId,
                missionName = $"Batalla de {sectorName}",
                sectorName = sectorName,
                isVictory = isVictory,
                completionPercentage = Mathf.Clamp(completionPercentage, 0f, 100f),
                accuracyPercentage = Mathf.Clamp(accuracy, 0f, 100f),
                enemiesDestroyed = enemiesDestroyed,
                alliedCasualties = alliedCasualties,
                civilianImpact = isVictory ? "Refugios y pistas a salvo al 100%" : "Almacenes principales dañados al 45%",
                warReason = warReason,
                date = todayDate,
                year = "1944",
                edition = $"EDICIÓN EXTRA N° {UnityEngine.Random.Range(40, 99)}"
            };

            RecordMission(report);
        }

        private static void LoadFromDisk()
        {
            cachedReports = new List<MissionReportData>();

            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREFS_KEY);
                    var wrapper = JsonUtility.FromJson<ReportListWrapper>(json);
                    if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                    {
                        cachedReports = wrapper.items;
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[HemerotecaManager] Error al cargar reportes guardados: {e.Message}");
                }
            }
        }

        private static void SaveToDisk()
        {
            if (cachedReports == null) return;
            try
            {
                var wrapper = new ReportListWrapper { items = cachedReports };
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HemerotecaManager] Error al guardar reportes: {e.Message}");
            }
        }

        /// <summary>
        /// Restablece los reportes a los valores de prueba originales.
        /// </summary>
        public static void ResetReports()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            cachedReports = null;
            OnReportsUpdated?.Invoke();
        }
    }
}
