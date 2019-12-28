# Encrypted Message System RPC Interface

EMS contains a basic RPC interface for performing common tasks and integrating EMS into other services and frontends.

All RPC functions return a consistent JSON formatted response, consisting of 2 root fields, `response` and `status`.  

``` json
{ 
   "response":{ 
      ...
   },
   "status":"OK"
}
```

The status field is either `OK` or `ERROR` depending on success or failure of the RPC request. In addition the RPC interface returns a http status code of 200 for success and 400 for failure. This provides 2 simple methods of checking if an RPC function has been successful. If returning http code 400 or JSON status `ERROR` the response should be discarded.

## RPC functions

Below is a list of the available RPC functions and an example of making the RPC request in bash and expected response. Examples bash scripts are also contained in `./rpc-test`.  
Examples assume that the node is started with `--rpc-port 5000`

## **get_address**

Returns the address currently in use by the node. [Bash example](./rpc-test/get_address)

**Response**
``` json
{ 
    "response":{ 
        "public":"NqeVhcQPYH8ttKcygShHcgtvTQXgqayk8bVudkFRJaVYezdSzaktyQWavw59F4ehU2gCZUJPUDoYuFYKDpSD16VH",
        "public_hex":"04444ecddeddce4b9640ad1b73219376852efcccdad9f9428c5f5a913affb1e29fced7f829e93f5c361e07b1eda31326215cf62df06dce6c392f6917ce00e7053c",
        "private":"jNDGXhGL5DH31UHMwRW9qVUssyCLSyfbhSj7QgDm7h",
        "private_hex":"2fe72f6e9078ab6d80b9af90e098718d1997a361f6fd6956ccb598ec41ebe4"
    },
    "status":"OK"
}
```

**Input:**  

- `private`: Should the response include the private keys?

**Output:**  

- `public`: Base58 encoded public key. This is the address people use to send you messages.  
- `public_hex`: Hex representation of the public key.  
- `private`: Base58 encoded private key. This is required to restore your address.  
- `private_hex`: Hex representation of the private key.  

*Note: if `private` is set to false in the request, both `private` and `private_hex` response fields will be `null`*

## **get_message_count**

Returns a count of the messages in the pool. [Bash example](./rpc-test/get_message_count)

**Response**
``` json
{ 
    "response":{ 
        "encrypted":1,
        "incoming":1,
        "outgoing":0
    },
    "status":"OK"
}
```

**Input:**  

- None

**Output:**    

- `encrypted`: The number of messages in the encrypted message pool.  
- `incoming`: The number of incoming messages. i.e. The number of messages you have received.  
- `outgoing`: The number of outgoing messages. i.e. The number of messages you have sent.

## **get_message_details**

Gets basic details of messages in the pool

**Response**  
``` json
{ 
    "response":{ 
        "encrypted":[ 
            { 
                "hash":"3bfd760da41fb55b6cf8fb7c23f90191"
            },
            { 
                "hash":"cd5c09f8bc8c6141275fc7bb37d078c3"
            }
        ],
        "incoming":[ 
            { 
                "sender":"NLje9jsC5ukYTDqo18hdwXiShjMMshcR8nAxUGYEAVGwbde9Fy2sTgkZwJVfxwgK1JqKes5oQVzNQDqNijQzrHzd",
                "hash":"3bfd760da41fb55b6cf8fb7c23f90191"
            }
        ],
        "outgoing":[ 
            { 
                "recipient":"NLje9jsC5ukYTDqo18hdwXiShjMMshcR8nAxUGYEAVGwbde9Fy2sTgkZwJVfxwgK1JqKes5oQVzNQDqNijQzrHzd",
                "hash":"cd5c09f8bc8c6141275fc7bb37d078c3"
            }
        ]
    },
    "status":"OK"
}
```

**Input:**  

- None

**Output:**  

- `encrypted`: An array of encrypted messages in the pool.   
    - `hash`: The hash of the message.  
- `incoming`: An array of incoming message items.    
    - `hash`: The hash of the message.  
    - `sender`: The address that sent the message to you.  
- `outgoing`: An array of outgoing message items.  
    - `hash`: The hash of the message.
    - `recipient`: The address you sent the message to.  

## **get_message**

Retrieves a message to read. [Bash example](./rpc-test/get_message)

**Response**  
``` json
{ 
    "response":{ 
        "incoming":true,
        "timestamp":1577523451,
        "destination":"NLje9jsC5ukYTDqo18hdwXiShjMMshcR8nAxUGYEAVGwbde9Fy2sTgkZwJVfxwgK1JqKes5oQVzNQDqNijQzrHzd",
        "message":"SGVsbG8gV29ybGQ="
    },
    "status":"OK"
}
```

**Input:**  

- `key`: The hash key of the message you want to read. Obtained with the `get_message_details` RPC endpoint

**Output:**  

- `incoming`: Flag indicating if this is an incoming or outgoing message.  
- `timestamp`: The Unix timestamp of the time the message was created.  
- `destination`: The address that authored the message. 
- `message`: The text of the message

*NOTE: The [get_message](./rpc-test/get_message) function returns a base64 encoded string. The test script demonstrates the process.*  

*NOTE: destination changes depending on the `incoming` flag. If true, `destination` is the address that sent you the message. If false, it is your address.*

## **send_message**

Send a message to another user. [Bash example](./rpc-test/send_message)

**Response**  
``` json
{
    "response": {
        "result": "36774ad4af5018e7931419af91ad0bf5"
    },
    "status": "OK"
}
```

**Input:**  

- `address`: The address to send the message to.  
- `message`: The message to send.

**Output:**  

- `key`: The hash key of the newly created message.  

*NOTE: The [send_message](./rpc-test/send_message) function expects the message data a base64 encoded string. The test script demonstrates the process.*