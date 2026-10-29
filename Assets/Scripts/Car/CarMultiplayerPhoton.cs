using Car;
using UnityEngine;
using Photon.Pun;
[RequireComponent(typeof(Photon.Pun.PhotonView))]
public class CarMultiplayerPhoton : MonoBehaviour, IPunObservable
{
    private PhotonView _photonView;
    private CarController _carController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Starting CarMultiplayerPhoton");
        _photonView = GetComponent<PhotonView>();
        _carController = GetComponent<CarController>();
        if(!_photonView.IsMine)
        {
            // Do something when this is not the local player's car
            _carController.SetRemoteControlled(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            // Send data to other players
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(_carController.GetVelocity());
            stream.SendNext(_carController.GetAngularVelocity());
            stream.SendNext(_carController.GetWheelYPositions());
            stream.SendNext(_carController.GetWheelSteeringAngle());
        }
        else
        {
            // Receive data from other players
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            _carController.SetVelocity((Vector3)stream.ReceiveNext());
            _carController.SetAngularVelocity((Vector3)stream.ReceiveNext());
            _carController.SetWheelYPositions((Vector4)stream.ReceiveNext());
            _carController.SetWheelSteeringAngle((float)stream.ReceiveNext());
        }
    }
}
