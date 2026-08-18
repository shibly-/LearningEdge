import http from 'k6/http';
import { sleep } from 'k6';

export let options = {
  vus: 20,           // number of virtual users
  duration: '5s',   // total test duration
};

export default function () {
  http.get('http://host.docker.internal:5087/api/v1/User/629E9956-A285-4386-B494-DC595A483A1D'); // use host.docker.internal for local API
  sleep(1);  
}  