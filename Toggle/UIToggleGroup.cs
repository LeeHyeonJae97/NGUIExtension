using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIToggleGroup : MonoBehaviour
{
    [SerializeField]
    protected List<UIToggle> _toggles;

    bool _initialized;

    public System.Action<int> OnSet;
    public System.Action<int, bool> OnChange;
    public System.Action<int> OnUnset;

    IEnumerator Start()
    {
        // NOTE :
        // 초기화 순서가 꼬이는 걸 방지하기 위해 한 프레임 뒤에 호출
        //
        yield return null;

        Initialize(0, false);
    }

    public void Initialize(bool notify)
    {
        Initialize(0, notify);
    }

    public void Initialize(int flag, bool notify)
    {
        if (!Validate() || _initialized) return;

        _initialized = true;

        for (int i = 0; i < _toggles.Count; i++)
        {
            EventDelegate.Add(_toggles[i].onChange, new EventDelegate(OnChange));

            _toggles[i].startsActive = (flag >> i & 1) == 1;
            _toggles[i].startOnChange = notify;
            _toggles[i].Start();

            _toggles[i].group = 0;
        }

        void OnChange()
        {
            var toggle = UIToggle.current;
            var index = _toggles.FindIndex((p) => p == toggle);

            if (toggle.value)
            {
                OnSet?.Invoke(index);
            }

            this.OnChange?.Invoke(index, toggle.value);

            if (!toggle.value)
            {
                OnUnset?.Invoke(index);
            }
        }
    }

    public void Set(int index, bool value, bool notify = true)
    {
        if (!Validate(index)) return;

        _toggles[index].Set(value, notify);
    }

    public void SetAll(bool value, bool notify = true)
    {
        foreach (var toggle in _toggles)
        {
            toggle.Set(value, notify);
        }
    }

    public void SetInteractable(bool value)
    {
        if (!Validate()) return;

        foreach (var toggle in _toggles)
        {
            toggle.GetComponentInChildren<BoxCollider>().enabled = value;
        }
    }

    public void SetInteractable(int index, bool value)
    {
        if (!Validate(index)) return;

        _toggles[index].GetComponentInChildren<BoxCollider>().enabled = value;
    }

    bool Validate()
    {
        return _toggles != null && _toggles.Count > 0;
    }

    bool Validate(int index)
    {
        return _toggles != null && _toggles.Count > 0 && 0 <= index && index < _toggles.Count;
    }
}
