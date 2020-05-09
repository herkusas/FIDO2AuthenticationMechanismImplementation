document.getElementById('register').addEventListener('submit', handleRegisterSubmit);

async function handleRegisterSubmit(event) {
    event.preventDefault();

    const username = this.username.value;
    const displayName = this.displayName.value;
    const attestationType = "none";
    const authenticatorAttachment = "";
    const userVerification = "required";

    const data = {
        username: username,
        displayName: displayName,
        attType: attestationType,
        authType: authenticatorAttachment,
        userVerification: userVerification,
        requireResidentKey: false
    };
    let makeCredentialOptions;

    try {
        makeCredentialOptions = await fetchCredentialOptions(data);
    } catch (serverException) {
        console.log(serverException);
        showErrorAlert("Nepavyko pasiekti serverio");
    }

    if (makeCredentialOptions) {
        if (makeCredentialOptions.status !== "ok") {
            console.log(makeCredentialOptions.errorMessage);
            showErrorAlert("Toks vartotojas jau egzistuoja, pabandykite kitą slapyvardį");
            return;
        }

        makeCredentialOptions.challenge = coerceToArrayBuffer(makeCredentialOptions.challenge);

        makeCredentialOptions.user.id = coerceToArrayBuffer(makeCredentialOptions.user.id);

        makeCredentialOptions.excludeCredentials = makeCredentialOptions.excludeCredentials.map((c) => {
            c.id = coerceToArrayBuffer(c.id);
            return c;
        });

        if (makeCredentialOptions.authenticatorSelection.authenticatorAttachment === null) makeCredentialOptions.authenticatorSelection.authenticatorAttachment = undefined;

    }

    showInformationAlert("Registruojama", "Sekite naršyklės instrukcijomis");

    let newCredential;
    try {
        newCredential = await navigator.credentials.create({
            publicKey: makeCredentialOptions
        });
    } catch (navigatorException) {
        console.log(navigatorException);
        showErrorAlert("Oeracija atšaukta");
    }

    if (newCredential)
        try {
            registerNewCredential(newCredential);

        } catch (e) {
            showErrorAlert("Nepavyko verifikuoti duomenų");
        }
}

async function fetchCredentialOptions(formData) {
    const response = await fetch(host + '/makeCredentialOptions', {
        method: 'POST',
        mode: 'cors',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
    });

    const data = await response.json();

    return data;
}


async function registerNewCredential(newCredential) {
    const attestationObject = new Uint8Array(newCredential.response.attestationObject);
    const clientDataJson = new Uint8Array(newCredential.response.clientDataJSON);
    const rawId = new Uint8Array(newCredential.rawId);

    const data = {
        id: newCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: newCredential.type,
        extensions: newCredential.getClientExtensionResults(),
        response: {
            AttestationObject: coerceToBase64Url(attestationObject),
            clientDataJson: coerceToBase64Url(clientDataJson)
        }
    };

    let response;
    try {
        response = await registerCredentialWithServer(data);
    } catch (serverException) {
        showErrorAlert(serverException);
        showErrorAlert("Nepavyko pasiekti serverio");
    }

    if (response)
        if (response.status !== "ok") {
            showErrorAlert(response.errorMessage);
            return;
        }
        else
        showSuccessAlert("Užsiregistravote", "Sveikiname Jūs sėkmingai užsiregistravote sistemoje");

    return;
}

async function registerCredentialWithServer(formData) {
    const response = await fetch(host + '/makeCredential', {
        method: 'POST',
        body: JSON.stringify(formData),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    });

    const data = await response.json();

    return data;
}