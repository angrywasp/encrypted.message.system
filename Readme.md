# Encrypted Message System

Encrypted Message System (EMS) is a decentralized, peer-to-peer messaging system. Each node acts as a store for messages in a collective cloud and to relay connection and message information betwen the network. 

## Encryption

EMS creates a key ring for each user which is a collection of asymmetric encryption keys based on secp256k1 elliptic curve cryptography. the public key forms the public address that other users can use to send you a message. Messages are encrypted/decrypted with a shared key that is created with the senders private key and recipients public key. the shared key is used as a symmetric key for encrypting the message content with AES. Users are able to determine if a message belongs to them by creating their own shared key to decrypt the message.

## Message Format

The message is contructed of the following data

- P2P Header  
    - P2P signature  
    - P2P Protocol version
    - Request byte
        - `Indicates if this message is sent to/from a server`
        - `Always 0x01 to mask the direction of the message`
    - Peer ID
    - Command byte 
        - `Indicates the type of data to expect in the message body`
        - `Byte code 0x0B`
    - Data length

- Message Body
    - Timestamp (ulong)
    - Address-XOR (65 bytes)
    - Encrypted message signature length (ushort)
    - Encrypted message length (ushort)
    - Message signature (Variable length)
    - Encrypted message content (Variable length)

## Decryption

As noted above, the nessage contains an `Address-XOR`. This is the product of the XOR function between the sender and receiver public key. To validate a message a potential recipient must first xor their public key with this field to obtain the senders public key. This can then be used to verify the message signature. If this message was not intended for you, the xor function will return a different key than was used to sign the message and the verification process will fail.

Iof verification is successful, the senders public key (used to verify the signature) is used to created a shared key with the recipients private key. this shared key will be the same as the one used to encrypt the message and should therefore decrypt the message successfully. 

## Privacy Considerations

To prevent tracability, messages will still be propagated after they have been successfully decrypted. this is to prevent tracing the destination of any message. All messages will continue to be propagated to and from all nodes until the message expires, at which time each node removes it from it's own local message pool.

Messages include a Peer ID as part of the P2P protocol header. To Prevent tracing messages from their origin, each node changes the peer ID to their own in the message header before forwarding it. This makes it so at any time in message propagation the Peer ID in the header only shows the last node it was relayed through. This also means that the same message on the network can have multiple different Peer ID values attached to it.

The Address-XOR value is a source of tracability. It exists to embed a return address for a message and a key to use to verify the message signature. sending with the same address to the same address will result in this field being the same and messages can be easily traced. Therefore every outgoing message should use a new address to change the value of the `address-xor` field. Future work will enforce this at the application level by automatically generating a new single use address for every outgoing message.

## TODO

The todo list is too long at this point, however in broad terms, future work should focus on 3 key areas

- Key ring enhancement and the enforcement of single use keys
- Spam prevention
- Message lifetime and pool pruning