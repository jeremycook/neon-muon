function warn(message: string, ...data: any[]) {
    console.warn(message, ...data);
    // TODO: Upload to log
}

function error(message: string, ...data: any[]) {
    console.error(message, ...data);
    // TODO: Upload to log
}

export const log = {
    error,
    warn,
}