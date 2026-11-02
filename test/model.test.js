import test from 'node:test';
import assert from 'node:assert/strict';
import {newTask,setStatus,matches,baseColumns} from '../src/model.js';
test('creation and completion transitions',()=>{const t=newTask();assert.equal(t.status,'进行中');assert.deepEqual(t.people,['王淞晖']);assert.ok(t.created);assert.equal(t.due,'');setStatus(t,'已完成');const completed=t.completed;assert.ok(completed);setStatus(t,'已完成');assert.equal(t.completed,completed);setStatus(t,'挂起');assert.equal(t.completed,'');});
test('participant filtering preserves everyone',()=>{const t=newTask();t.people.push('李明');assert.ok(matches(t,{people:'李明'},baseColumns));assert.equal(matches(t,{people:'李'},baseColumns),false);assert.deepEqual(t.people,['王淞晖','李明']);});
test('inclusive dates, empty dates, combined custom filters',()=>{const t=newTask();t.due='2026-11-02';t.custom.project='研发';const cols=[...baseColumns,{key:'project',type:'text'}];assert.ok(matches(t,{due:{from:'2026-11-02',to:'2026-11-02'},project:'研'},cols));assert.equal(matches(t,{due:{from:'2026-11-03'}},cols),false);t.due='';assert.equal(matches(t,{due:{to:'2026-11-03'}},cols),false);});
