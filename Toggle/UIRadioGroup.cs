using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRadioGroup : MonoBehaviour
{
    [SerializeField]
    protected int _default;

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

        Initialize(_default, false);
    }

    public void Initialize(int index, bool notify)
    {
        if (!Validate(index) || _initialized) return;

        _initialized = true;

        int groupId = GetInstanceID();

        for (int i = 0; i < _toggles.Count; i++)
        {
            EventDelegate.Add(_toggles[i].onChange, new EventDelegate(OnChange));

            _toggles[i].startsActive = i == index;
            _toggles[i].startOnChange = notify;
            _toggles[i].Start();

            _toggles[i].group = groupId;
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

    public void Set(int index, bool notify = true)
    {
        if (!Validate(index)) return;

        _toggles[index].Set(true, notify);
    }

    public void SetDefault(bool notify = true)
    {
        Set(_default, notify);
    }

    public void SetActive(bool value)
    {
        if (!Validate()) return;

        foreach (var toggle in _toggles)
        {
            toggle.gameObject.SetActive(value);
        }
    }

    public void SetActive(int index, bool value)
    {
        if (!Validate(index)) return;

        _toggles[index].gameObject.SetActive(value);
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
