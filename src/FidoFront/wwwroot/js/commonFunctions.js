const host = "http://localhost:51938";

coerceToArrayBuffer = function (thing, name) {
    if (typeof thing === "string") {
        thing = thing.replace(/-/g, "+").replace(/_/g, "/");
        const str = window.atob(thing);
        const bytes = new Uint8Array(str.length);
        for (let i = 0; i < str.length; i++) {
            bytes[i] = str.charCodeAt(i);
        }
        thing = bytes;
    }

    if (Array.isArray(thing)) {
        thing = new Uint8Array(thing);
    }

    if (thing instanceof Uint8Array) {
        thing = thing.buffer;
    }

    if (!(thing instanceof ArrayBuffer)) {
        throw new TypeError(`could not coerce '${name}' to ArrayBuffer`);
    }

    return thing;
};

coerceToBase64Url = function (thing) {
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }

    if (thing instanceof Uint8Array) {
        let str = "";
        const len = thing.byteLength;

        for (let i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }

    if (typeof thing !== "string") {
        throw new Error("could not coerce to string");
    }

    thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

    return thing;
};

function showErrorAlert(message) {
    window.Swal.fire({
        icon: "error",
        title: "Klaida",
        text: message
    });
}

function showInformationAlert(title, message) {
    window.Swal.fire({
        title: title,
        text: message,
        icon: "info",
        showConfirmButton: false
});
}

function showSuccessAlert(title, message) {
    window.Swal.fire({
        title: title,
        text: message,
        icon: "success"
    });
}

function value(selector) {
    const el = document.querySelector(selector);
    if (el.type === "checkbox") {
        return el.checked;
    }
    return el.value;
}
