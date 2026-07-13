#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "PassiveDefinition", menuName = "Project/Data/Passive Definition")]
    public sealed class PassiveDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [TabGroup("Metadata")]
        [LabelText("Id")]
        [SerializeField] private string _Id = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Display Name")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Description")]
        [SerializeField, TextArea] private string _Description = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Icon")]
        [PreviewField(64)]
        [SerializeField] private Sprite _Icon;

        [TabGroup("Resource")]
        [LabelText("Uses Treasure Resource")]
        [SerializeField] private bool _UsesTreasureResource;

        [TabGroup("Resource")]
        [ShowIf(nameof(_UsesTreasureResource))]
        [LabelText("Max Treasure")]
        [SerializeField, Min(1)] private int _MaxTreasure = 5;

        [TabGroup("Resource")]
        [ShowIf(nameof(_UsesTreasureResource))]
        [LabelText("Treasure Stack State")]
        [SerializeField] private StateDefinition _TreasureStackState;

        [TabGroup("Resource")]
        [ShowIf(nameof(_UsesTreasureResource))]
        [LabelText("Full Treasure State")]
        [SerializeField] private StateDefinition _FullTreasureState;

        [TabGroup("Pactole")]
        [ShowIf(nameof(_UsesTreasureResource))]
        [LabelText("Refund Treasure On Pactole Hit")]
        [SerializeField] private bool _RefundTreasureOnPactoleHit;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public Sprite Icon => _Icon;
        public bool UsesTreasureResource => _UsesTreasureResource;
        public int MaxTreasure => Mathf.Max(1, _MaxTreasure);
        public StateDefinition TreasureStackState => _TreasureStackState;
        public StateDefinition FullTreasureState => _FullTreasureState;
        public bool RefundTreasureOnPactoleHit => _UsesTreasureResource && _RefundTreasureOnPactoleHit;

        #endregion
    }
}
