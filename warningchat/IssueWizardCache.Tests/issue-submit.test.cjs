const fs = require('fs');
const vm = require('vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../warningchat/wwwroot/js/warning/transgression.js'), 'utf8');
async function scenario(id, response, expectedNavigation, redirectUrl) {
  let submit, prevented = false, fetches = 0, navigation, activeStep, alert;
  const button = { disabled: false, textContent: 'Save & Continue' };
  const form = { dataset: {}, action: '/Transgression/CreateTransgression',
    querySelector: s => s.startsWith('input') ? {value:id} : button,
    addEventListener: (_, handler) => { submit = handler; } };
  const context = {
    document: { readyState: 'complete', getElementById: id => id === 'frmTransgression' ? form : null },
    window: { location: {href: 'https://example.test/Transgression', assign: url => navigation = url} },
    sessionStorage: {setItem: (key, value) => activeStep = value},
    FormData: class { set() {} }, URL,
    fetch: async () => { fetches++; return {ok:(response.success ?? response.Success), status:(response.success ?? response.Success) ? 200 : 400,
      redirected: !!redirectUrl, url: redirectUrl, headers:{get:()=> redirectUrl ? 'text/html' : 'application/json'}, json: async()=>response}; },
    console: {warn(){},error(){}}, alert: msg => alert = msg
  };
  vm.runInNewContext(source, context);
  await submit({preventDefault:()=> prevented=true});
  assert.equal(prevented,true);
  assert.equal(fetches,1);
  assert.equal(button.disabled,false);
  if (expectedNavigation) {
    assert.equal(new URL(navigation).searchParams.get('id'),'42');
    assert.equal(activeStep,'2');
    assert.equal(alert,undefined);
  } else {
    assert.equal(navigation,undefined);
    assert.ok(alert);
  }
}
(async()=>{
  await scenario('',{success:true,warningId:42},true);
  await scenario('42',{success:true,warningId:42},true);
  await scenario('',{success:false,message:'Please select an employee.'},false);
  await scenario('',{success:true},false);
  await scenario('',{Success:true,WarningId:42},true);
  await scenario('',{success:true},true,'https://example.test/Transgression/Index/42');
  await scenario('',{success:true},true,'https://example.test/Transgression?id=42');
  await scenario('',{success:true},false,'https://example.test/Account/Login');
  await scenario('',{success:true},false,'https://other.test/Transgression/Index/42');
  console.log('PASS: 9 save scenarios including JSON casing, legacy redirects, login redirects and untrusted redirect origins.');
})().catch(e=>{console.error(e);process.exitCode=1});
