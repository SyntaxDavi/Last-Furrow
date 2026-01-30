using System;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Instância runtime de uma Tradição.
    /// Similar a CardInstance - representa uma tradição específica equipada pelo jogador.
    /// 
    /// ARQUITETURA:
    /// - TraditionData = template (ScriptableObject imutável)
    /// - TraditionInstance = instância (estado runtime, mutável)
    /// </summary>
    [Serializable]
    public class TraditionInstance
    {
        [SerializeField] private string _traditionID;
        [SerializeField] private string _instanceID;
        
        // Estado runtime (não serializado por padrão)
        [NonSerialized] private TraditionData _cachedData;
        [NonSerialized] private bool _isActive = true;
        
        /// <summary>
        /// ID da TraditionData (usado para lookup no GameLibrary).
        /// </summary>
        public string TraditionID => _traditionID;
        
        /// <summary>
        /// ID único desta instância (para identificar entre múltiplas cópias).
        /// </summary>
        public string InstanceID => _instanceID;
        
        /// <summary>
        /// Dados da tradição (requer lookup no GameLibrary).
        /// </summary>
        public TraditionData Data
        {
            get
            {
                if (_cachedData == null && !string.IsNullOrEmpty(_traditionID))
                {
                    // TODO: Lookup via GameLibrary quando implementado
                    // _cachedData = GameLibrary.GetTradition(_traditionID);
                }
                return _cachedData;
            }
        }
        
        /// <summary>
        /// Se a tradição está ativa (pode ser desativada temporariamente por alguns efeitos).
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        
        // Construtor padrão para serialização
        public TraditionInstance() { }
        
        /// <summary>
        /// Cria uma nova instância de tradição.
        /// </summary>
        public TraditionInstance(TraditionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            _traditionID = data.ID;
            _instanceID = GenerateInstanceID();
            _cachedData = data;
            _isActive = true;
        }
        
        /// <summary>
        /// Cria instância a partir de ID (para carregar de save).
        /// </summary>
        public TraditionInstance(string traditionID)
        {
            _traditionID = traditionID;
            _instanceID = GenerateInstanceID();
            _isActive = true;
        }
        
        /// <summary>
        /// Injeta referência do Data após carregar de save.
        /// </summary>
        public void HydrateData(TraditionData data)
        {
            if (data != null && data.ID == _traditionID)
            {
                _cachedData = data;
            }
        }
        
        private string GenerateInstanceID()
        {
            return $"{_traditionID}_{Guid.NewGuid():N}".Substring(0, 16);
        }
        
        public override string ToString()
        {
            return $"[Tradition: {_traditionID} ({_instanceID})]";
        }
    }
}
