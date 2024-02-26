using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class ServerList : MonoBehaviour
{
    private readonly Dictionary<IPAddress, Button> _ipVisualizer = new(new IPAddressEqualityComparer());
    private readonly HashSet<IPAddress> _servers = new(new IPAddressEqualityComparer());

    public event Action<IPAddress> OnClickServer;
    public Button buttonPrefab;

    public ISet<IPAddress> Servers
    {
        get => _servers;
        set
        {
            foreach (var server in _servers.Except(value).ToArray())
            {
                RemoveButton(server);
                _servers.Remove(server);
            }

            foreach (var server in value.Except(_servers))
            {
                _servers.Add(server);
                MakeButton(server);
            }
        }
    }

    private void RemoveButton(IPAddress server)
    {
        Destroy(_ipVisualizer[server].gameObject);
        _ipVisualizer.Remove(server);
    }

    private void MakeButton(IPAddress server)
    {
        var button = Instantiate(buttonPrefab, transform).GetComponent<Button>();
        button.onClick.AddListener(() => OnClickServer?.Invoke(server));
        button.GetComponentInChildren<TMP_Text>().text = server.ToString();
        _ipVisualizer[server] = button;
    }
}
