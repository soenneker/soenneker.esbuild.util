'use strict';
const readline = require('node:readline');
const esbuild = require(process.argv[1]);
let context;
const send = value => process.stdout.write(JSON.stringify(value, (key, value) =>
  value instanceof Uint8Array ? Buffer.from(value).toString('base64') : value) + '\n');
const options = value => {
  if (!value) return {};
  for (const key of ['mangleProps', 'reserveProps'])
    if (typeof value[key] === 'string') value[key] = new RegExp(value[key]);
  return value;
};
async function handle(request) {
  const { id, operation, payload } = request;
  try {
    let result;
    switch (operation) {
      case 'version': result = esbuild.version; break;
      case 'build': result = await esbuild.build(options(payload)); break;
      case 'transform': result = await esbuild.transform(payload.input, options(payload.options)); break;
      case 'analyze': result = await esbuild.analyzeMetafile(payload.metafile, payload.options); break;
      case 'format': result = await esbuild.formatMessages(payload.messages, payload.options); break;
      case 'context': context = await esbuild.context(options(payload)); break;
      case 'rebuild': result = await context.rebuild(); break;
      case 'watch': await context.watch(payload || {}); break;
      case 'serve': result = await context.serve(payload || {}); break;
      case 'cancel': await context.cancel(); break;
      case 'dispose': if (context) await context.dispose(); esbuild.stop(); break;
      default: throw new Error(`Unknown operation: ${operation}`);
    }
    send({ id, result: result ?? null });
  } catch (error) {
    send({ id, error: { message: error.message, errors: error.errors || [], warnings: error.warnings || [] } });
  }
}
const input = readline.createInterface({ input: process.stdin });
input.on('line', line => {
  try { void handle(JSON.parse(line)); }
  catch (error) { process.stderr.write(error.message + '\n'); process.exitCode = 1; input.close(); }
});
input.on('close', async () => { if (context) await context.dispose(); esbuild.stop(); });
