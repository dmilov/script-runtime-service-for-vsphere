import { Component, Inject, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home-component',
  templateUrl: './home.component.html'  
})
export class HomeComponent {  
  public vcaddress: string;
  public thumbprint: string;
  public btnHidden = false;
  public registerInProgress: boolean;
  closeModal: string;
  baseUrl: string;
  http: HttpClient

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
    this.http = http;
    this.registerInProgress = false;
  }

  public onRegister() {
    this.registerInProgress = true;
    this.http.get<string>(this.baseUrl + 'servercertificate',
      {
        params: {
          serverAddress: this.vcaddress
        }
      }
    ).subscribe((data: string) => {
      this.thumbprint = data;      

    }, error => console.error(error));
  }
}
