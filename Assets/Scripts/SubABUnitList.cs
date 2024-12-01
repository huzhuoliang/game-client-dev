using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace DefaultNamespace {
    [Serializable]
    public class SubABUnitList : IDisposable {
        private Dictionary<string, SubABUnit> _abUnitDic = new();

        [ShowInInspector]
        [HideReferenceObjectPicker]
        [LabelText("AssetBundle Unit List")]
        [Searchable]
        [ListDrawerSettings(IsReadOnly = true)]
        private List<SubABUnit> _abUnitList = new();

        public bool Contains(string bundleName) {
            return _abUnitDic.ContainsKey(bundleName);
        }

        public void Add(string bundleName, SubABUnit abUnit) {
            if (abUnit == null || Contains(bundleName)) {
                return;
            }

            _abUnitDic[bundleName] = abUnit;
            _abUnitList.Add(abUnit);
        }

        public bool TryGetValue(string bundleName, out SubABUnit abUnit) {
            abUnit = null;
            return _abUnitDic.TryGetValue(bundleName, out abUnit);
        }

        public void Dispose() {
            foreach (SubABUnit ab in _abUnitList.Where(ab => !ReferenceEquals(ab, null))) {
                ab.Dispose();
            }
        }
    }
}
