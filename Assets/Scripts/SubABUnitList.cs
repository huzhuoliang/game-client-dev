using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Game;
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

        public bool Contains([NotNull] AssetBundleInfoUnit unit) {
            if (unit == null) {
                throw new ArgumentNullException(nameof(unit));
            }

            return Contains(unit.BundleName);
        }

        public void Add(SubABUnit abUnit) {
            if (abUnit == null || Contains(abUnit.BundleName)) {
                return;
            }

            _abUnitDic[abUnit.BundleName] = abUnit;
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

            _abUnitList.Clear();
        }
    }
}
