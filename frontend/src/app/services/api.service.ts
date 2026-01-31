import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import {
  Location,
  WeatherResponse,
  RecommendationResponse,
  ChatRequest,
  ChatResponse
} from '../models/api.models';
import { environment } from '../../environments/environment';

/**
 * Service for communicating with the backend API
 */
@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /**
   * Get all available locations
   */
  getLocations(): Observable<Location[]> {
    return this.http.get<Location[]>(`${this.baseUrl}/locations`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get a location by ID
   */
  getLocation(id: number): Observable<Location> {
    return this.http.get<Location>(`${this.baseUrl}/locations/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get weather data by coordinates
   */
  getWeatherByCoordinates(lat: number, lon: number, date?: Date): Observable<WeatherResponse> {
    let params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lon.toString());
    
    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }

    return this.http.get<WeatherResponse>(`${this.baseUrl}/weather`, { params }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get weather data by city name
   */
  getWeatherByCity(city: string, date?: Date): Observable<WeatherResponse> {
    let params = new HttpParams();
    
    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }

    return this.http.get<WeatherResponse>(`${this.baseUrl}/weather/city/${encodeURIComponent(city)}`, { params }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get recommendations by location ID
   */
  getRecommendations(locationId: number, date?: Date): Observable<RecommendationResponse> {
    let params = new HttpParams().set('locationId', locationId.toString());
    
    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }

    return this.http.get<RecommendationResponse>(`${this.baseUrl}/recommendations`, { params }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get recommendations by city name
   */
  getRecommendationsByCity(city: string, date?: Date): Observable<RecommendationResponse> {
    let params = new HttpParams();
    
    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }

    return this.http.get<RecommendationResponse>(`${this.baseUrl}/recommendations/city/${encodeURIComponent(city)}`, { params }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Send a chat message and get AI-powered response
   */
  sendChatMessage(message: string): Observable<ChatResponse> {
    const request: ChatRequest = { message };
    return this.http.post<ChatResponse>(`${this.baseUrl}/chat`, request).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Check chat service status
   */
  getChatStatus(): Observable<{ llmAvailable: boolean; message: string }> {
    return this.http.get<{ llmAvailable: boolean; message: string }>(`${this.baseUrl}/chat/status`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.error?.detail) {
        errorMessage = error.error.detail;
      } else if (error.status) {
        errorMessage = `Error ${error.status}: ${error.statusText}`;
      }
    }
    
    console.error('API Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }
}
