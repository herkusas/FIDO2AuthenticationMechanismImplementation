﻿document.getElementById('authenticate').addEventListener('submit', handleSignInSubmit);

async function handleSignInSubmit(event) {
    event.preventDefault();
    const username = this.username.value;
    const formData = new FormData();
    formData.append('username', username);

    let assertionOptions;

    try {
        assertionOptions = await fetchAssertionOptions(formData);
    } catch (serverException) {
        console.log(serverException);
        showErrorAlert("Nepavyko pasiekti serverio");
    }

    if (assertionOptions) {
        if (assertionOptions.status !== "ok") {
            showErrorAlert(assertionOptions.errorMessage);
            return;
        }

        const challenge = assertionOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
        assertionOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

        assertionOptions.allowCredentials.forEach(function (listItem) {
            const fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
            listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
        });
    }

    showInformationAlert("Autentifikuojama", "Sekite naršyklės instrukcijomis");

    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: assertionOptions });
    } catch (navigatorException) {
        console.log(navigatorException);
        showErrorAlert("Operacija atšaukta");
    }

    if (credential)
        try {
            await verifyAssertionWithServer(credential);
        } catch (assertionException) {
            console.log(assertionException);
            showErrorAlert("Nepavyko verifikuoti duomenų");
        }
}

async function fetchAssertionOptions(formData) {
    const response = await fetch(host + '/assertionOptions', {
        method: 'POST',
        body: formData,
        headers: {
            'Accept': 'application/json'
        }
    });

    const data = await response.json();

    return data;
}


async function verifyAssertionWithServer(assertedCredential) {
    const authData = new Uint8Array(assertedCredential.response.authenticatorData);
    const clientDataJson = new Uint8Array(assertedCredential.response.clientDataJSON);
    const rawId = new Uint8Array(assertedCredential.rawId);
    const sig = new Uint8Array(assertedCredential.response.signature);
    const data = {
        id: assertedCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: assertedCredential.type,
        extensions: assertedCredential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData),
            clientDataJson: coerceToBase64Url(clientDataJson),
            signature: coerceToBase64Url(sig)
        }
    };

    let response;
    try {
        const res = await fetch(host + '/makeAssertion', {
            method: 'POST',
            body: JSON.stringify(data),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        response = await res.json();
    } catch (serverException) {
        console.log(serverException);
        showErrorAlert("Nepavyko pasiekti serverio");
        return;
    }

    if(response)
    if (response.status !== "ok") {
        console.log(response.errorMessage);
        showErrorAlert("Toks vartotojas nėra registruotas sistemoje");
        return;
    }
    else
            showSuccessAlert("Prisijungėte", "Sveikiname sėkmingai prisijungus prie sistemos");

    setTimeout(function () {
        location.href = response.redirectionUri;
    }, 2000);
}