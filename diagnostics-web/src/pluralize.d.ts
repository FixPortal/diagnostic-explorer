// The `pluralize` package ships no bundled types and we consume only its
// default callable form. Declaring the default export keeps the import working
// under strict mode without esModuleInterop, and avoids depending on the Node
// `require` global from app code that is also compiled by the spec build.
declare module 'pluralize' {
    const pluralize: (word: string, count?: number, inclusive?: boolean) => string;
    export default pluralize;
}
