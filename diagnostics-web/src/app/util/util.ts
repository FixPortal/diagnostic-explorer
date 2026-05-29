export function strEqCI(s1: string | null, s2: string | null): boolean {
    if (s1 === null && s2 === null)
        return true;

    if (s1 === undefined && s2 === undefined)
        return true;

    if (s1 === null || s2 === null)
        return false;

    if (s1 === undefined || s2 === undefined)
        return false;

    return s1.localeCompare(s2, undefined, {sensitivity: 'base'}) === 0;
}

export function getBaseLocation() {
    // slice (non-mutating) of the first path segment; splice was returning the removed element by
    // luck. Still only supports a single-segment base path, which is all the current deploy uses.
    let paths: string[] = location.pathname.split('/').slice(1, 2);
    let basePath: string = (paths && paths[0]) || '';
    if (!basePath.endsWith('/'))
        basePath += '/';
    if (!basePath.startsWith('/'))
        basePath = '/' + basePath;
    return basePath;
}

export function getErrorMessage(err: any): string {
    if (typeof (err) === 'string')
        return err;

    return (err.error?.exceptionMessage ?? err.error?.message ?? err.message ?? '').toString();
}

export function today(): Date {
    let now: Date = new Date();
    now.setHours(0, 0, 0, 0);
    return now;
}

